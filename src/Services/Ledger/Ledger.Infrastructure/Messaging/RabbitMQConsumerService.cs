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
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
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
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }

                switch (eventType)
                {
                    case "PaymentAuthorizedDomainEvent":
                        var authEvent = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<CreateLedgerAccountHandler>();
                            await handler.Handle(new CreateLedgerAccountCommand(authEvent.MerchantId, authEvent.Amount.Currency), cancellationToken);
                        }
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<ReserveFundsHandler>();
                            var result = await handler.Handle(new ReserveFundsCommand(authEvent.MerchantId, authEvent.Amount.Amount, authEvent.Amount.Currency, correlationId ?? $"PaymentAuth:{authEvent.IntentId}"), cancellationToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("ReserveFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, cancellationToken);
                                return;
                            }
                        }
                        break;

                    case "PaymentCapturedDomainEvent":
                        var captureEvent = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<CaptureFundsHandler>();
                            var result = await handler.Handle(new CaptureFundsCommand(captureEvent.MerchantId, captureEvent.Amount.Amount, captureEvent.Amount.Currency, correlationId ?? $"PaymentCapt:{captureEvent.IntentId}"), cancellationToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("CaptureFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, cancellationToken);
                                return;
                            }
                        }
                        break;

                    case "PaymentRefundedDomainEvent":
                        var refundEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var handler = scope.ServiceProvider.GetRequiredService<RefundFundsHandler>();
                            var result = await handler.Handle(new RefundFundsCommand(refundEvent.MerchantId, refundEvent.Amount.Amount, refundEvent.Amount.Currency, correlationId ?? $"PaymentRef:{refundEvent.IntentId}"), cancellationToken);
                            if (result.IsFailure)
                            {
                                _logger.LogWarning("RefundFunds failed: {Errors}", string.Join("; ", result.Errors.Select(e => e.Message)));
                                await HandleFailureAsync(ea, messageId, cancellationToken);
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
                    await db.SaveChangesAsync(cancellationToken);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Processed event {EventType} ({MessageId})", eventType, messageId);
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
        // A delivered message always carries BasicProperties; the client types it
        // as nullable so we assert it rather than branch on a value that cannot be null.
        var basicProperties = ea.BasicProperties!;
        // The consumer is running on a channel created in ExecuteAsync, so it is
        // always initialized by the time a failure can be handled.
        var channel = _channel ?? throw new InvalidOperationException("Channel is not initialized.");
        var retryCount = MessageRetryPolicy.GetRetryCount(basicProperties.Headers);

        if (MessageRetryPolicy.ShouldRetry(retryCount))
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = basicProperties.MessageId,
                CorrelationId = basicProperties.CorrelationId,
                Headers = new Dictionary<string, object?>
                {
                    [MessageRetryPolicy.RetryCountHeader] = retryCount + 1
                }
            };

            await channel.BasicPublishAsync(
                exchange: _retryExchange,
                routingKey: ea.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body,
                cancellationToken: cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

            _logger.LogWarning("Message {MessageId} failed; scheduled retry {RetryCount}/{MaxRetries}",
                messageId, retryCount + 1, MessageRetryPolicy.MaxRetries);
        }
        else
        {
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = basicProperties.MessageId,
                CorrelationId = basicProperties.CorrelationId,
                Headers = basicProperties.Headers
            };

            await channel.BasicPublishAsync(
                exchange: _dlxExchange,
                routingKey: ea.RoutingKey,
                mandatory: false,
                basicProperties: properties,
                body: ea.Body,
                cancellationToken: cancellationToken);
            await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);

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
