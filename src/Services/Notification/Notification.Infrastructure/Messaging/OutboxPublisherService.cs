using BuildingBlocks.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Persistence;

namespace Notification.Infrastructure.Messaging;

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
                await ProcessOutbox(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutbox(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

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

            foreach (var msg in messages)
            {
                try
                {
                    using var activity = RabbitMqTracing.StartProducerActivity(
                        msg.EventType, RabbitMqTracing.ParseTraceParent(msg.TraceParent));
                    await bus.PublishAsync(msg.EventType, msg.Payload, msg.Id.ToString(), msg.CorrelationId, cancellationToken);
                    msg.MarkAsProcessed();
                    _logger.LogInformation("Published outbox message {Id} ({Type})", msg.Id, msg.EventType);
                }
                catch (Exception ex)
                {
                    msg.ReleaseLease();
                    _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
                }
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}