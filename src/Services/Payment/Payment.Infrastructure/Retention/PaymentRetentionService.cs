using BuildingBlocks.Shared.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Retention;

/// <summary>
/// Periodic archival sweep for the Payment service: moves expired payment intents
/// (with their owned transaction history, 7y financial retention) and processed
/// outbox messages (7d) into the <c>ArchivedRecords</c> JSON-snapshot table before
/// deleting the originals. Payments are retained per regulatory timelines rather
/// than discarded.
/// </summary>
public class PaymentRetentionService : RetentionCleanupServiceBase<PaymentRetentionOptions>
{
    public PaymentRetentionService(
        IServiceScopeFactory scopeFactory,
        IOptions<PaymentRetentionOptions> options,
        ILogger<PaymentRetentionService> logger)
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
            db, db.ArchivedRecords, "PaymentIntents", "PaymentIntents",
            db.PaymentIntents.Where(p => p.CreatedAt < businessCutoff)
                .OrderBy(p => p.CreatedAt)
                .Take(batchSize),
            p => p.Id, cancellationToken);

        total += await DataArchiveWriter.ArchiveAsync(
            db, db.ArchivedRecords, "OutboxMessages", "OutboxMessages",
            db.OutboxMessages.Where(m => m.Processed && m.OccurredOn < messageCutoff)
                .OrderBy(m => m.OccurredOn)
                .Take(batchSize),
            m => m.Id, cancellationToken);

        if (total > 0)
            Logger.LogInformation("Archived {Count} Payment record(s)", total);

        return total;
    }
}