using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Middleware;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Application.Features.Commands.RefundFunds;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Ledger.Infrastructure.Messaging;

public class RabbitMQConsumerService : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly string _queueName = "ledger.payment.events";
    private readonly string _retryExchange = "ledger.payment.events.retry";
    private readonly string _retryQueue = "ledger.payment.events.retry";
    private readonly string _dlxExchange = "ledger.payment.events.dlx";
    private readonly string _dlq = "ledger.payment.events.dlq";
    private readonly string _sourceExchange = "payment.events";

    public RabbitMQConsumerService(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<RabbitMQConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _correlationIdProvider = correlationIdProvider;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(_retryExchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(_dlxExchange, ExchangeType.Topic, durable: true).GetAwaiter().GetResult();

        var retryArgs = new Dictionary<string, object?>
        {
            ["x-message-ttl"] = (long)MessageRetryPolicy.RetryDelay.TotalMilliseconds,
            ["x-dead-letter-exchange"] = _sourceExchange
        };
        _channel.QueueDeclareAsync(_retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: retryArgs).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_retryQueue, _retryExchange, "#").GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(_dlq, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_dlq, _dlxExchange, "#").GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentAuthorizedDomainEvent", null).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentCapturedDomainEvent", null).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_queueName, _sourceExchange, "PaymentRefundedDomainEvent", null).GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
            var correlationId = ea.BasicProperties.CorrelationId;
            var eventType = ea.RoutingKey;
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                if (!string.IsNullOrWhiteSpace(correlationId))
                {
                    _correlationIdProvider.Set(correlationId);
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var existing = db.InboxMessages.FirstOrDefault(m => m.MessageId == messageId);
                    if (existing is not null && existing.ProcessedAt is not null)
                    {
                        _logger.LogWarning("Duplicate message {MessageId} ignored", messageId);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    if (existing is null)
                    {
                        db.InboxMessages.Add(new InboxMessage(messageId, eventType, body));
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                switch (eventType)
                {
                    case "PaymentAuthorizedDomainEvent":
                        var authEvent = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<CreateLedgerAccountHandler>();
                            await handler.Handle(new CreateLedgerAccountCommand(authEvent.MerchantId, authEvent.Amount.Currency), stoppingToken);
                        }
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<ReserveFundsHandler>();
                            var result = await handler.Handle(new ReserveFundsCommand(authEvent.MerchantId, authEvent.Amount.Amount, authEvent.Amount.Currency, correlationId ?? $"PaymentAuth:{authEvent.IntentId}"), stoppingToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("ReserveFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, stoppingToken);
                                return;
                            }
                        }
                        break;

                    case "PaymentCapturedDomainEvent":
                        var captureEvent = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<CaptureFundsHandler>();
                            var result = await handler.Handle(new CaptureFundsCommand(captureEvent.MerchantId, captureEvent.Amount.Amount, captureEvent.Amount.Currency, correlationId ?? $"PaymentCapt:{captureEvent.IntentId}"), stoppingToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("CaptureFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, stoppingToken);
                                return;
                            }
                        }
                        break;

                    case "PaymentRefundedDomainEvent":
                        var refundEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<RefundFundsHandler>();
                            var result = await handler.Handle(new RefundFundsCommand(refundEvent.MerchantId, refundEvent.Amount.Amount, refundEvent.Amount.Currency, correlationId ?? $"PaymentRef:{refundEvent.IntentId}"), stoppingToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("RefundFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, stoppingToken);
                                return;
                            }
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventType);
                        break;
                }

                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var msg = db.InboxMessages.First(m => m.MessageId == messageId);
                    msg.MarkAsProcessed();
                    await db.SaveChangesAsync(stoppingToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Processed event {EventType} ({MessageId})", eventType, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await HandleFailureAsync(ea, messageId, stoppingToken);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
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

    public override void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
