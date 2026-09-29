using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Shared.BackgroundServices;

/// <summary>
/// Shared pump loop for polling background workers (outbox publishers, webhook
/// dispatcher, inbox/reporting cleanup). Guarantees a graceful shutdown: once
/// the host cancels <paramref name="stoppingToken"/> the loop stops scheduling
/// new work and runs one final best-effort drain of the current workload before
/// returning, so an in-flight batch completes instead of being abandoned mid-run.
///
/// Workloads that are inherently at-least-once (a DB-leased outbox, a durable
/// webhook delivery table) rely on <see cref="RunAsync"/>'s drain to flush the
/// remaining backlog; anything that survives the process restart is retried by
/// the lease/backoff machinery.
/// </summary>
public static class WorkerLoop
{
    public static async Task RunAsync(
        Func<CancellationToken, Task> processOnce,
        TimeSpan pollInterval,
        ILogger logger,
        CancellationToken stoppingToken,
        string? operationName = null)
    {
        operationName ??= "worker";

        while (true)
        {
            try
            {
                await processOnce(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutdown requested mid-iteration. The workload's persistence
                // guarantees (lease, durable queue) mean unprocessed items are
                // retried later, so we exit without error noise.
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in {OperationName}", operationName);
            }

            if (stoppingToken.IsCancellationRequested)
                break;

            try
            {
                await Task.Delay(pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Final flush: still cancelled, so no new work is scheduled, but
                // give the current workload one more bounded pass to complete.
                // processOnce must be bounded (a single batch), never loop forever.
                await processOnce(CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Best-effort drain of {OperationName} at shutdown failed", operationName);
            }
        }
    }
}