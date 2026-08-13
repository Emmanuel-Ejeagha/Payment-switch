using System.Diagnostics.Metrics;
using System.Text;
using BuildingBlocks.Shared.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notification.Infrastructure.DeadLetter;

/// <summary>
/// Consumes <c>notification.events.dlq</c> and persists every dead-lettered
/// message (payload, event type, retry metadata, reason), so exhausted messages
/// are never silently dropped. Exposes Prometheus signals: a counter of
/// dead-lettered messages and a gauge of the current DLQ depth.
/// </summary>
public class DeadLetterConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadLetterConsumerService> _logger;
    private readonly RabbitMQSettings _settings;
    private const string QueueName = "notification.events.dlq";
    private const string DlqExchange = "notification.events.dlx";

    private static readonly Meter Meter = new("PaymentSwitch", "1.0.0");
    private static readonly Counter<long> DeadLetterCounter =
        Meter.CreateCounter<long>("notification_dead_letters_total", description: "Notification messages recorded from the dead-letter queue.");
    private static readonly Gauge<long> DlqDepth =
        Meter.CreateGauge<long>("notification_dlq_depth", description: "Current depth of the notification dead-letter queue.");

    public DeadLetterConsumerService(
        IOptions<RabbitMQSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<DeadLetterConsumerService> logger)
    {
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dead-letter consumer error. Retrying in 10 seconds...");
            }

            await Task.Delay(10_000, stoppingToken);
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

        var connection = await factory.CreateConnectionAsync(cancellationToken);
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(DlqExchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(QueueName, DlqExchange, "#", null, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
            var eventType = ea.RoutingKey;
            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var retryCount = MessageRetryPolicy.GetRetryCount(ea.BasicProperties.Headers);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.DeadLetterRecords.Add(new DeadLetterRecord(
                    messageId, eventType, QueueName, "retries-exhausted", retryCount, payload));
                await db.SaveChangesAsync(cancellationToken);

                DeadLetterCounter.Add(1);
                _logger.LogWarning("Recorded dead-lettered message {MessageId} ({EventType}) after {RetryCount} retries",
                    messageId, eventType, retryCount);

                await channel.BasicAckAsync(ea.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record dead-lettered message {MessageId}; leaving on queue for retry", messageId);
            }
        };

        await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (connection is not { IsOpen: true } || channel is not { IsOpen: true })
            {
                _logger.LogWarning("RabbitMQ connection lost; reconnecting in 10 seconds...");
                break;
            }

            try
            {
                var declare = await channel.QueueDeclarePassiveAsync(QueueName, cancellationToken);
                DlqDepth.Record(declare.MessageCount);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to poll DLQ depth");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}