using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Middleware;
using BuildingBlocks.Shared.Results;
using Ledger.Application.Features.Commands.CaptureFunds;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Application.Features.Commands.RefundFunds;
using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
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
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer error. Retrying in 10 seconds...");
            }

            try
            {
                await Task.Delay(10_000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task TryConnectAndConsume(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
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
            // Resume the producer's trace (HTTP/outbox → broker → this consumer → DB).
            using var activity = RabbitMqTracing.StartConsumerActivity(
                ea.RoutingKey,
                RabbitMqTracing.ExtractActivityContext(ea.BasicProperties.Headers));
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
                    var existing = await db.InboxMessages.FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
                    if (existing is { State: InboxState.Processed })
                    {
                        _logger.LogWarning("Duplicate message {MessageId} ignored", messageId);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    // Claim the message in the SAME transaction the handler commits,
                    // so the inbox row and the ledger posting are flushed atomically.
                    if (existing is null)
                        db.InboxMessages.Add(new InboxMessage(messageId, eventType, body));
                    else
                        existing.Reclaim();

                    var result = await ProcessEventAsync(scope, eventType, body, correlationId, cancellationToken);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning("Event {EventType} ({MessageId}) failed: {Errors}",
                            eventType, messageId, string.Join("; ", result.Errors.Select(e => e.Message)));
                        await HandleFailureAsync(ea, messageId, cancellationToken);
                        return;
                    }
                }

                await MarkAsProcessedAsync(messageId, cancellationToken);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Processed event {EventType} ({MessageId})", eventType, messageId);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // A prior delivery committed the posting but crashed before marking
                // the inbox row processed. The unique CorrelationId index makes the
                // re-run a no-op; treat it as an idempotent completion.
                _logger.LogWarning("Duplicate posting detected for {MessageId}; completing idempotently", messageId);
                await MarkAsProcessedAsync(messageId, cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await HandleFailureAsync(ea, messageId, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);

        // Keep the connection alive until cancelled. If the broker drops mid-run
        // the connection closes; exit so ExecuteAsync reconnects and re-declares
        // the topology instead of blocking on a dead channel.
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_connection is not { IsOpen: true })
            {
                _logger.LogWarning("RabbitMQ connection lost; reconnecting in 10 seconds...");
                break;
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task<Result> ProcessEventAsync(
        IServiceScope scope,
        string eventType,
        string body,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        switch (eventType)
        {
            case "PaymentAuthorizedDomainEvent":
                var authEvent = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
                var createHandler = scope.ServiceProvider.GetRequiredService<CreateLedgerAccountHandler>();
                var createResult = await createHandler.Handle(
                    new CreateLedgerAccountCommand(authEvent.MerchantId, authEvent.Amount.Currency), cancellationToken);
                if (createResult.IsFailure)
                    return createResult;
                var reserveHandler = scope.ServiceProvider.GetRequiredService<ReserveFundsHandler>();
                return await reserveHandler.Handle(
                    new ReserveFundsCommand(authEvent.MerchantId, authEvent.Amount.Amount, authEvent.Amount.Currency,
                        correlationId ?? $"PaymentAuth:{authEvent.IntentId}"), cancellationToken);

            case "PaymentCapturedDomainEvent":
                var captureEvent = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
                var captureHandler = scope.ServiceProvider.GetRequiredService<CaptureFundsHandler>();
                return await captureHandler.Handle(
                    new CaptureFundsCommand(captureEvent.MerchantId, captureEvent.Amount.Amount, captureEvent.Amount.Currency,
                        correlationId ?? $"PaymentCapt:{captureEvent.IntentId}"), cancellationToken);

            case "PaymentRefundedDomainEvent":
                var refundEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
                var refundHandler = scope.ServiceProvider.GetRequiredService<RefundFundsHandler>();
                return await refundHandler.Handle(
                    new RefundFundsCommand(refundEvent.MerchantId, refundEvent.Amount.Amount, refundEvent.Amount.Currency,
                        correlationId ?? $"PaymentRef:{refundEvent.IntentId}"), cancellationToken);

            default:
                _logger.LogWarning("Unknown event type: {EventType}", eventType);
                return Result.Success();
        }
    }

    private async Task MarkAsProcessedAsync(string messageId, CancellationToken cancellationToken)
    {
        // A fresh scope so a failed handler's dirty tracked entities can never be re-saved.
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var msg = await db.InboxMessages.FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
        if (msg is null || msg.State == InboxState.Processed)
            return;

        msg.MarkAsProcessed();
        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            || ex.InnerException?.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
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

    /// <summary>
    /// Graceful teardown: after the consume loop exits, close the channel and
    /// connection so any in-flight unacknowledged deliveries are requeued by the
    /// broker (autoAck is off), never dropped. Async avoids sync-over-async.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            await _channel.CloseAsync(CancellationToken.None);
            await _channel.DisposeAsync();
        }
        if (_connection is not null)
        {
            await _connection.CloseAsync(CancellationToken.None);
            await _connection.DisposeAsync();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}
