using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Shared.Retention;

/// <summary>
/// Shared pump loop for the per-service retention sweep. Each iteration computes
/// the business and messaging cutoffs from <typeparamref name="TOptions"/> and
/// delegates the copy-then-delete batch to <see cref="CleanupBatchAsync"/>.
/// <see cref="RunOnceAsync"/> is the single entry point used by both the loop and
/// integration tests, so the exact archival path is exercised directly.
/// </summary>
public abstract class RetentionCleanupServiceBase<TOptions> : BackgroundService
    where TOptions : class, IRetentionOptions
{
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly IOptions<TOptions> Options;
    protected readonly ILogger Logger;

    protected RetentionCleanupServiceBase(
        IServiceScopeFactory scopeFactory,
        IOptions<TOptions> options,
        ILogger logger)
    {
        ScopeFactory = scopeFactory;
        Options = options;
        Logger = logger;
    }

    /// <summary>
    /// Archives one batch of expired rows for this service's tables. Implementations
    /// use <see cref="DataArchiveWriter"/> so copy + delete stay in one transaction.
    /// </summary>
    protected abstract Task<int> CleanupBatchAsync(
        DateTime businessCutoff,
        DateTime messageCutoff,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>Runs one archival sweep. Public so tests can drive the service directly.</summary>
    public async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var businessCutoff = now - TimeSpan.FromDays(Options.Value.BusinessRetentionDays);
        var messageCutoff = now - TimeSpan.FromDays(Options.Value.MessageRetentionDays);
        return await CleanupBatchAsync(businessCutoff, messageCutoff, Options.Value.BatchSize, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, Options.Value.CleanupIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Retention archival sweep failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }
}