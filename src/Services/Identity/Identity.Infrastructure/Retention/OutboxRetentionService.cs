using BuildingBlocks.Shared.Retention;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Retention;

/// <summary>
/// Periodic sweep for the Identity outbox: hard-deletes processed rows older
/// than the messaging retention window.
///
/// Deliberate deviation from the archive-then-delete pattern used by the
/// Notification retention worker: outbox rows are transport envelopes, not
/// business history (the facts live in Users and downstream services), and
/// verification-event payloads carry a raw single-use token whose lifetime
/// must be bounded everywhere — including datastores. Unprocessed or leased
/// rows are never touched (they still have work to do).
/// </summary>
public class OutboxRetentionService : RetentionCleanupServiceBase<OutboxRetentionOptions>
{
    public OutboxRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxRetentionOptions> options,
        ILogger<OutboxRetentionService> logger)
        : base(scopeFactory, options, logger)
    {
    }

    protected override async Task<int> CleanupBatchAsync(
        DateTime businessCutoff,
        DateTime messageCutoff,
        int batchSize,
        CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var deleted = await db.OutboxMessages
            .Where(m => m.Processed && m.OccurredOn < messageCutoff)
            .OrderBy(m => m.OccurredOn)
            .Take(batchSize)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            Logger.LogInformation("Deleted {Count} processed Identity outbox row(s)", deleted);

        return deleted;
    }
}
