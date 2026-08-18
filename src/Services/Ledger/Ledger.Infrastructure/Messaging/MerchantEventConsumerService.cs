using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Middleware;
using BuildingBlocks.Shared.Results;
using Ledger.Application.Features.Commands.CreateLedgerAccount;
using Ledger.Infrastructure.Inbox;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Ledger.Infrastructure.Messaging;

/// <summary>
/// Consumes merchant lifecycle events off <c>merchant.events</c>. Merchant
/// onboarding provisions a default-currency ledger account so funds can be
/// reserved against it before the merchant's first payment arrives. The source
/// exchange is declared here (idempotent) so the first bind succeeds even if the
/// Merchant API has not published yet.
/// </summary>
public class MerchantEventConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ILogger<MerchantEventConsumerService> _logger;
    private readonly RabbitMQSettings _settings;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _queueName = "ledger.merchant.events";
    private readonly string _retryExchange = "ledger.merchant.events.retry";
    private readonly string _retryQueue = "ledger.merchant.events.retry";
    private readonly string _dlxExchange = "ledger.merchant.events.dlx";
    private readonly string _dlq = "ledger.merchant.events.dlq";
    private readonly string _sourceExchange = "merchant.events";

    public MerchantEventConsumerService(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ICorrelationIdProvider correlationIdProvider,
        ILogger<MerchantEventConsumerService> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _correlationIdProvider = correlationIdProvider;
        _logger = logger;
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
                _logger.LogError(ex, "Merchant event consumer error. Retrying in 10 seconds...");
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

        await _channel.ExchangeDeclareAsync(_sourceExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
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
        await _channel.QueueBindAsync(_queueName, _sourceExchange, "MerchantOnboardedEvent", null, cancellationToken: cancellationToken);

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

                    // Claim the message alongside the handler's write so a duplicate
                    // delivery can be told apart (Reclaim) from a first pass.
                    if (existing is null)
                        db.InboxMessages.Add(new InboxMessage(messageId, eventType, body));
                    else
                        existing.Reclaim();

                    var result = await ProcessOnboardAsync(scope, body, cancellationToken);
                    if (result.IsFailure)
                    {
                        _logger.LogWarning("MerchantOnboardedEvent ({MessageId}) failed: {Errors}",
                            messageId, string.Join("; ", result.Errors.Select(e => e.Message)));
                        await HandleFailureAsync(ea, messageId, cancellationToken);
                        return;
                    }
                }

                await MarkAsProcessedAsync(messageId, cancellationToken);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                _logger.LogInformation("Processed MerchantOnboardedEvent ({MessageId})", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", messageId);
                await HandleFailureAsync(ea, messageId, cancellationToken);
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_connection is not { IsOpen: true })
            {
                _logger.LogWarning("Merchant event consumer connection lost; reconnecting in 10 seconds...");
                break;
            }
            await Task.Delay(1000, cancellationToken);
        }
    }

    private async Task<Result> ProcessOnboardAsync(IServiceScope scope, string body, CancellationToken cancellationToken)
    {
        var onboardEvent = JsonSerializer.Deserialize<MerchantOnboardedPayload>(body)!;
        var handler = scope.ServiceProvider.GetRequiredService<CreateLedgerAccountHandler>();
        // The onboarding payload carries no default currency, so provision the
        // account in the platform default; the merchant's first payment currency
        // provisions additional accounts via the payment consumer.
        return await handler.Handle(new CreateLedgerAccountCommand(onboardEvent.MerchantId, "USD"), cancellationToken);
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

    private async Task HandleFailureAsync(BasicDeliverEventArgs ea, string messageId, CancellationToken cancellationToken)
    {
        var basicProperties = ea.BasicProperties!;
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
    /// broker (autoAck is off), never dropped.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_channel is not null)
        {
            try
            {
                await _channel.CloseAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or RabbitMQ.Client.Exceptions.AlreadyClosedException)
            {
                // The client library already closed the channel (e.g. the
                // connection dropped mid-run); teardown must not fail the shutdown.
            }
            await _channel.DisposeAsync();
        }
        if (_connection is not null)
        {
            try
            {
                await _connection.CloseAsync(CancellationToken.None);
            }
            catch (Exception ex) when (ex is ObjectDisposedException or RabbitMQ.Client.Exceptions.AlreadyClosedException)
            {
            }
            await _connection.DisposeAsync();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}