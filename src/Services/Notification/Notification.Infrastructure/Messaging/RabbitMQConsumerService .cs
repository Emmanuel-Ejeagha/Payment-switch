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
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly string _queueName = "notification.events";

    public RabbitMQConsumerService(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<RabbitMQConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, "payment.events", "PaymentAuthorizedDomainEvent", null).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, "payment.events", "PaymentCapturedDomainEvent", null).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, "payment.events", "PaymentRefundedDomainEvent", null).GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
                await db.SaveChangesAsync(stoppingToken);

                // Map event to notification command
                CreateNotificationCommand? command = eventType switch
                {
                    "PaymentAuthorizedDomainEvent" => MapAuthorized(body),
                    "PaymentCapturedDomainEvent" => MapCaptured(body),
                    "PaymentRefundedDomainEvent" => MapRefunded(body),
                    _ => null
                };

                if (command != null)
                {
                    var result = await createHandler.Handle(command, stoppingToken);
                    if (result.IsSuccess)
                        _logger.LogInformation("Created notification for {EventType} ({MessageId})", eventType, messageId);
                }

                inboxMsg.MarkAsProcessed();
                await db.SaveChangesAsync(stoppingToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private CreateNotificationCommand MapAuthorized(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
        return new CreateNotificationCommand(
            "customer@example.com", // placeholder – later retrieve from merchant/customer data
            "email",
            "Payment Authorized",
            $"Your payment of {e.Amount.Amount} {e.Amount.Currency} has been authorized.",
            null,
            body
        );
    }

    private CreateNotificationCommand MapCaptured(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
        return new CreateNotificationCommand(
            "customer@example.com",
            "email",
            "Payment Captured",
            $"Your payment of {e.Amount.Amount} {e.Amount.Currency} has been captured.",
            null,
            body
        );
    }

    private CreateNotificationCommand MapRefunded(string body)
    {
        var e = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
        return new CreateNotificationCommand(
            "customer@example.com",
            "email",
            "Payment Refunded",
            $"Your refund of {e.Amount.Amount} {e.Amount.Currency} has been processed.",
            null,
            body
        );
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