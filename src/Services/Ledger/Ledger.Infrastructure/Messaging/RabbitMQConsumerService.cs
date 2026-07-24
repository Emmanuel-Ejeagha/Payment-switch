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
    private readonly ILogger<RabbitMQConsumerService> _logger;
    private readonly string _queueName = "ledger.payment.events";

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
            var createAccountHandler = scope.ServiceProvider.GetRequiredService<CreateLedgerAccountHandler>();
            var reserveHandler = scope.ServiceProvider.GetRequiredService<ReserveFundsHandler>();
            var captureHandler = scope.ServiceProvider.GetRequiredService<CaptureFundsHandler>();
            var refundHandler = scope.ServiceProvider.GetRequiredService<RefundFundsHandler>();

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

                switch (eventType)
                {
                    case "PaymentAuthorizedDomainEvent":
                        var authEvent = JsonSerializer.Deserialize<PaymentAuthorizedEvent>(body)!;
                        await createAccountHandler.Handle(new CreateLedgerAccountCommand(authEvent.MerchantId, authEvent.Amount.Currency), stoppingToken);
                        await reserveHandler.Handle(new ReserveFundsCommand(authEvent.MerchantId, authEvent.Amount.Amount, authEvent.Amount.Currency, $"PaymentAuth:{authEvent.IntentId}"), stoppingToken);
                        break;

                    case "PaymentCapturedDomainEvent":
                        var captureEvent = JsonSerializer.Deserialize<PaymentCapturedEvent>(body)!;
                        await captureHandler.Handle(new CaptureFundsCommand(captureEvent.MerchantId, captureEvent.Amount.Amount, captureEvent.Amount.Currency, $"PaymentCapt:{captureEvent.IntentId}"), stoppingToken);
                        break;

                    case "PaymentRefundedDomainEvent":
                        var refundEvent = JsonSerializer.Deserialize<PaymentRefundedEvent>(body)!;
                        await refundHandler.Handle(new RefundFundsCommand(refundEvent.MerchantId, refundEvent.Amount.Amount, refundEvent.Amount.Currency, $"PaymentRef:{refundEvent.IntentId}"), stoppingToken);
                        break;

                    default:
                        _logger.LogWarning("Unknown event type: {EventType}", eventType);
                        break;
                }

                inboxMsg.MarkAsProcessed();
                await db.SaveChangesAsync(stoppingToken);
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