using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Features.Commands.CreateNotification;
using Notification.Infrastructure.Inbox;
using Notification.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Notification.Infrastructure.Messaging;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _queueName = "notification.events";

    public RabbitMQConsumerService(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMQConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TryConnectAndConsume(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer error. Retrying in 10 seconds...");
            }

            await Task.Delay(10_000, stoppingToken);
        }
    }

    private async Task TryConnectAndConsume(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, "payment.events", "PaymentAuthorizedDomainEvent", null, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, "payment.events", "PaymentCapturedDomainEvent", null, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, "payment.events", "PaymentRefundedDomainEvent", null, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
            var eventType = ea.RoutingKey;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createHandler = scope.ServiceProvider.GetRequiredService<CreateNotificationHandler>();

            try
            {
                if (db.InboxMessages.Any(m => m.MessageId == messageId))
                {
                    _logger.LogWarning("Duplicate message {MessageId} ignored", messageId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var inboxMsg = new InboxMessage(messageId, eventType, body);
                db.InboxMessages.Add(inboxMsg);
                await db.SaveChangesAsync(cancellationToken);

                CreateNotificationCommand? command = eventType switch
                {
                    "PaymentAuthorizedDomainEvent" => MapAuthorized(body),
                    "PaymentCapturedDomainEvent" => MapCaptured(body),
                    "PaymentRefundedDomainEvent" => MapRefunded(body),
                    _ => null
                };

                if (command != null)
                {
                    var result = await createHandler.Handle(command, cancellationToken);
                    if (result.IsSuccess)
                        _logger.LogInformation("Created notification for {EventType} ({MessageId})", eventType, messageId);
                }

                inboxMsg.MarkAsProcessed();
                await db.SaveChangesAsync(cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);

        // Keep the connection alive until cancelled
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    private CreateNotificationCommand MapAuthorized(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
        return new CreateNotificationCommand("customer@example.com", "email", "Payment Authorized",
            $"Your payment of {e.Amount.Amount} {e.Amount.Currency} has been authorized.", null, body);
    }

    private CreateNotificationCommand MapCaptured(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
        return new CreateNotificationCommand("customer@example.com", "email", "Payment Captured",
            $"Your payment of {e.Amount.Amount} {e.Amount.Currency} has been captured.", null, body);
    }

    private CreateNotificationCommand MapRefunded(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
        return new CreateNotificationCommand("customer@example.com", "email", "Payment Refunded",
            $"Your refund of {e.Amount.Amount} {e.Amount.Currency} has been processed.", null, body);
    }

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

public record PaymentAuthorizedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, string AuthorizationCode, string GatewayReference);
public record PaymentCapturedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record PaymentRefundedEvent(Guid IntentId, Guid MerchantId, MoneyPayload Amount, Guid TransactionId);
public record MoneyPayload(decimal Amount, string Currency);