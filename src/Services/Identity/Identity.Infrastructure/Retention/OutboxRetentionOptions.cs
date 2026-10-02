using BuildingBlocks.Shared.Retention;

namespace Identity.Infrastructure.Retention;

public class OutboxRetentionOptions : IRetentionOptions
{
    public const string SectionName = "Retention";

    public int CleanupIntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int MessageRetentionDays { get; set; } = 7;

    /// <summary>
    /// Unused: Identity sweeps messaging rows only (no business-table archival).
    /// Required by <see cref="IRetentionOptions"/>.
    /// </summary>
    public int BusinessRetentionDays { get; set; } = 7;
}
