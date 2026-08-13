using BuildingBlocks.Shared.Messaging;
using BuildingBlocks.Shared.Observability;
using Identity.Application.Interfaces;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.Messaging;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(2);
    private readonly TimeSpan _leaseDuration = TimeSpan.FromSeconds(30);

    public OutboxPublisherService(IServiceScopeFactory scopeFactory, ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxMessages(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var leaseToken = Guid.NewGuid();
        var leaseUntil = DateTime.UtcNow.Add(_leaseDuration);

        await db.OutboxMessages
            .Where(m => !m.Processed && (m.LeaseExpiresAt == null || m.LeaseExpiresAt < DateTime.UtcNow))
            .ExecuteUpdateAsync(
                s => s.SetProperty(m => m.LeaseToken, leaseToken)
                      .SetProperty(m => m.LeaseExpiresAt, leaseUntil),
                cancellationToken);

        while (true)
        {
            var messages = await db.OutboxMessages
                .Where(m => m.LeaseToken == leaseToken)
                .OrderBy(m => m.OccurredOn)
                .Take(10)
                .ToListAsync(cancellationToken);

            if (messages.Count == 0)
                break;

            foreach (var message in messages)
            {
                try
                {
                    using var activity = RabbitMqTracing.StartProducerActivity(
                        message.EventType, RabbitMqTracing.ParseTraceParent(message.TraceParent));
                    await eventBus.PublishAsync(message.EventType, message.Payload, message.Id.ToString(), message.CorrelationId, cancellationToken);
                    message.MarkAsProcessed();
                    OutboxMetrics.RecordPublished();
                    _logger.LogInformation("Published outbox message {MessageId} of type {EventType}", message.Id, message.EventType);
                }
                catch (Exception ex)
                {
                    message.ReleaseLease();
                    OutboxMetrics.RecordFailed();
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        OutboxMetrics.SetBacklog(await db.OutboxMessages.CountAsync(m => !m.Processed, cancellationToken));
    }
}