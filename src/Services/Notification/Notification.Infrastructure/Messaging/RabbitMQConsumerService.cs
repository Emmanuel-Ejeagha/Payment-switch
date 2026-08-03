using BuildingBlocks.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Features.Commands.CreateNotification;
using Notification.Application.Interfaces;
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
    private readonly string _retryExchange = "notification.events.retry";
    private readonly string _retryQueue = "notification.events.retry";
    private readonly string _dlxExchange = "notification.events.dlx";
    private readonly string _dlq = "notification.events.dlq";
    private readonly string _sourceExchange = "payment.events";


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

        await _channel.ExchangeDeclareAsync(_retryExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(_dlxExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var retryArgs = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = (long)MessageRetryPolicy.RetryDelay.TotalMilliseconds,
            ["x-dead-letter-exchange"] = _sourceExchange
        };
        await _channel.QueueDeclareAsync(_retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_retryQueue, _retryExchange, "#", null, cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(_dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_dlq, _dlxExchange, "#", null, cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentAuthorizedDomainEvent", null, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentCapturedDomainEvent", null, cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentRefundedDomainEvent", null, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
            var eventType = ea.RoutingKey;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var createHandler = scope.ServiceProvider.GetRequiredService<CreateNotificationHandler>();
            var realTimeNotifier = scope.ServiceProvider.GetRequiredService<IRealTimeNotifier>();

            // Extract merchantId from the event for real-time notification
            Guid? merchantId = null;

            try
            {
                var existing = db.InboxMessages.FirstOrDefault(m => m.MessageId == messageId);
                if (existing is not null && existing.ProcessedAt is not null)
                {
                    _logger.LogWarning("Duplicate message {MessageId} ignored", messageId);
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                if (existing is null)
                {
                    var inboxMsg = new InboxMessage(messageId, eventType, body);
                    db.InboxMessages.Add(inboxMsg);
                    await db.SaveChangesAsync(cancellationToken);
                }

                CreateNotificationCommand? command = null;

                switch (eventType)
                {
                    case "PaymentAuthorizedDomainEvent":
                        var authEvent = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
                        command = MapAuthorized(body);
                        merchantId = authEvent.MerchantId;
                        break;

                    case "PaymentCapturedDomainEvent":
                        var captEvent = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
                        command = MapCaptured(body);
                        merchantId = captEvent.MerchantId;
                        break;

                    case "PaymentRefundedDomainEvent":
                        var refEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
                        command = MapRefunded(body);
                        merchantId = refEvent.MerchantId;
                        break;
                }

                if (command != null)
                {
                    var result = await createHandler.Handle(command, cancellationToken);
                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("Created notification for {EventType} ({MessageId})", eventType, messageId);

                        if (merchantId.HasValue)
                        {
                            await realTimeNotifier.NotifyPaymentEventAsync(
                                merchantId.Value,
                                eventType,
                                $"Payment {eventType} processed.",
                                cancellationToken);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("CreateNotification failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                        await HandleFailureAsync(ea, messageId, cancellationToken);
                        return;
                    }
                }

                var msg = db.InboxMessages.First(m => m.MessageId == messageId);
                msg.MarkAsProcessed();
                await db.SaveChangesAsync(cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await HandleFailureAsync(ea, messageId, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);

        // Keep the connection alive until cancelled
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task HandleFailureAsync(BasicDeliverEventArgs ea, string messageId, CancellationToken cancellationToken)
    {
        var retryCount = MessageRetryPolicy.GetRetryCount(ea.BasicProperties.Headers);

        if (MessageRetryPolicy.ShouldRetry(retryCount))
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = ea.BasicProperties.MessageId,
                CorrelationId = ea.BasicProperties.CorrelationId,
                Headers = new Dictionary<string, object?>
                {
                    [MessageRetryPolicy.RetryCountHeader] = retryCount + 1
                }
            };

            await _channel.BasicPublishAsync(
                exchange: _retryExchange,
                routingKey: ea.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body,
                cancellationToken: cancellationToken);
            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

            _logger.LogWarning("Message {MessageId} failed; scheduled retry {RetryCount}/{MaxRetries}",
                messageId, retryCount + 1, MessageRetryPolicy.MaxRetries);
        }
        else
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = ea.BasicProperties.MessageId,
                CorrelationId = ea.BasicProperties.CorrelationId,
                Headers = ea.BasicProperties.Headers
            };

            await _channel.BasicPublishAsync(
                exchange: _dlxExchange,
                routingKey: ea.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body,
                cancellationToken: cancellationToken);
            await _channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

            _logger.LogError("Message {MessageId} failed after {MaxRetries} retries; moved to DLQ",
                messageId, MessageRetryPolicy.MaxRetries);
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
public record MoneyPayload(long Amount, string Currency);
