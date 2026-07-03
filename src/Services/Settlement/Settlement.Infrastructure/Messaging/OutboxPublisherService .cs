using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Settlement.Application.Interfaces;
using Settlement.Infrastructure.Persistence;

namespace Settlement.Infrastructure.Messaging;

public class OutboxPublisherService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxPublisherService> _logger;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);

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
            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessOutbox(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var messages = await db.OutboxMessages
            .Where(m => !m.Processed)
            .OrderBy(m => m.OccurredOn)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var msg in messages)
        {
            try
            {
                await bus.PublishAsync(msg.EventType, msg.Payload, cancellationToken);
                msg.MarkAsProcessed();
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Published outbox message {Id} ({Type})", msg.Id, msg.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox message {Id}", msg.Id);
            }
        }
    }
}