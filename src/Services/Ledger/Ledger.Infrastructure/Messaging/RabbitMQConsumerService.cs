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
                    if (db.InboxMessages.Any(m => m.MessageId == messageId))
                    {
                        _logger.LogWarning("Duplicate message {MessageId} ignored", messageId);
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }

                    var inboxMsg = new InboxMessage(messageId, eventType, body);
                    db.InboxMessages.Add(inboxMsg);
                    await db.SaveChangesAsync(stoppingToken);
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
                                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
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
                                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
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
                                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
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
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false); // don't requeue
            }
        };

        await _channel.BasicConsumeAsync(_queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
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