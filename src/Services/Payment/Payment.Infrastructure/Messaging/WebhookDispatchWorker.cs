using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Messaging;

public class WebhookDispatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WebhookDispatchWorker> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 20;
    private const int MaxAttempts = 8;

    public WebhookDispatchWorker(IServiceScopeFactory scopeFactory, ILogger<WebhookDispatchWorker> logger)
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
                await ProcessPending(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending webhook deliveries");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPending(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var repository = scope.ServiceProvider.GetRequiredService<IWebhookEventRepository>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<Services.WebhookDispatcher>();

        var pending = await repository.GetPendingBatchAsync(DateTime.UtcNow, BatchSize, cancellationToken);
        foreach (var webhookEvent in pending)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var (success, error) = await dispatcher.DeliverAsync(webhookEvent, cancellationToken);
            if (success)
            {
                webhookEvent.MarkSucceeded();
                _logger.LogInformation("Delivered webhook {EventId} of type {EventType} to merchant {MerchantId}",
                    webhookEvent.Id, webhookEvent.EventType, webhookEvent.MerchantId);
            }
            else if (webhookEvent.Attempts + 1 >= MaxAttempts)
            {
                webhookEvent.MarkFailed(error, TimeSpan.Zero);
                _logger.LogWarning("Webhook {EventId} exceeded max attempts: {Error}", webhookEvent.Id, error);
            }
            else
            {
                var backoff = ComputeBackoff(webhookEvent.Attempts + 1);
                webhookEvent.MarkFailed(error, backoff);
                _logger.LogWarning("Webhook {EventId} failed (attempt {Attempt}): {Error}; retrying in {Backoff}",
                    webhookEvent.Id, webhookEvent.Attempts, error, backoff);
            }
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static TimeSpan ComputeBackoff(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 300));
}
