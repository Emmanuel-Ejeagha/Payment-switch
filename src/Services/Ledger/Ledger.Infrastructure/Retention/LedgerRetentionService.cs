using BuildingBlocks.Shared.Retention;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ledger.Infrastructure.Retention;

/// <summary>
/// Periodic archival sweep for the Ledger: moves expired journal entries (7y
/// financial retention) and processed inbox/outbox messages (7d) into the
/// <c>ArchivedRecords</c> JSON-snapshot table before deleting the originals.
/// Replaces the old hard-delete <c>InboxCleanupService</c>, so no audit data is
/// ever discarded — only relocated.
/// </summary>
public class LedgerRetentionService : RetentionCleanupServiceBase<LedgerRetentionOptions>
{
    public LedgerRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<LedgerRetentionOptions> options,
        ILogger<LedgerRetentionService> logger)
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

        var total = 0;
        total += await DataArchiveWriter.ArchiveAsync(
            db, db.ArchivedRecords, "JournalEntries", "JournalEntries",
            db.LedgerAccounts.SelectMany(a => a.Journal)
                .Where(j => j.Timestamp < businessCutoff)
                .OrderBy(j => j.Timestamp)
                .Take(batchSize),
            j => j.Id, cancellationToken);

        total += await DataArchiveWriter.ArchiveAsync(
            db, db.ArchivedRecords, "InboxMessages", "InboxMessages",
            db.InboxMessages.Where(m => m.ProcessedAt != null && m.ProcessedAt < messageCutoff)
                .OrderBy(m => m.ProcessedAt)
                .Take(batchSize),
            m => m.Id, cancellationToken);

        total += await DataArchiveWriter.ArchiveAsync(
            db, db.ArchivedRecords, "OutboxMessages", "OutboxMessages",
            db.OutboxMessages.Where(m => m.Processed && m.OccurredOn < messageCutoff)
                .OrderBy(m => m.OccurredOn)
                .Take(batchSize),
            m => m.Id, cancellationToken);

        if (total > 0)
            Logger.LogInformation("Archived {Count} Ledger record(s)", total);

        return total;
    }
}