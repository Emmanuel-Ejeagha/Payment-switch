using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Features.Command.CollectSubscriptionCycle;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Messaging;

/// <summary>
/// Polls for subscriptions whose next billing date has arrived and drives one
/// collection attempt each. Retry scheduling lives in the collect handler.
/// </summary>
public class SubscriptionBillingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionBillingWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromMinutes(1);
    private const int BatchSize = 20;

    public SubscriptionBillingWorker(IServiceScopeFactory scopeFactory, ILogger<SubscriptionBillingWorker> logger)
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
                await ProcessDue(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing due subscriptions");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessDue(CancellationToken cancellationToken)
    {
        List<Guid> dueIds;
        using (var lookupScope = _scopeFactory.CreateScope())
        {
            var repository = lookupScope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var due = await repository.GetDueBatchAsync(DateTime.UtcNow, BatchSize, cancellationToken);
            dueIds = due.Select(s => s.Id).ToList();
        }

        if (dueIds.Count == 0) return;

        _logger.LogInformation("Billing {Count} due subscription(s)", dueIds.Count);

        foreach (var subscriptionId in dueIds)
        {
            if (cancellationToken.IsCancellationRequested) break;

            // A fresh scope per subscription keeps one bad cycle from poisoning the rest of the batch.
            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<CollectSubscriptionCycleHandler>();

            try
            {
                var result = await handler.Handle(new CollectSubscriptionCycleCommand(subscriptionId), cancellationToken);
                if (!result.IsSuccess)
                    _logger.LogWarning("Could not bill subscription {SubscriptionId}: {Error}",
                        subscriptionId, result.Errors.First().Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error billing subscription {SubscriptionId}", subscriptionId);
            }
        }
    }
}
