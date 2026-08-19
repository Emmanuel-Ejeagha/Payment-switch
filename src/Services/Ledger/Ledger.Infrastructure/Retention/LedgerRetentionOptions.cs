using BuildingBlocks.Shared.Retention;

namespace Ledger.Infrastructure.Retention;

public class LedgerRetentionOptions : IRetentionOptions
{
    public const string SectionName = "Retention";

    public int CleanupIntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int MessageRetentionDays { get; set; } = 7;

    /// <summary>Journal entries are financial records; keep 7 years (2557 days).</summary>
    public int BusinessRetentionDays { get; set; } = 2557;
}