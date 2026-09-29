namespace BuildingBlocks.Shared.Retention;

/// <summary>
/// Configurable retention policy for a service's archival sweep: how long
/// business records and transient messaging rows are kept in the live tables
/// before being moved to the archive, how often the sweep runs, and how many
/// rows are moved per batch. Per-service implementations add their own default
/// <see cref="BusinessRetentionDays"/> (7 years for financial data, 90 days for
/// notifications, etc.).
/// </summary>
public interface IRetentionOptions
{
    /// <summary>How often the archival sweep runs, in minutes.</summary>
    int CleanupIntervalMinutes { get; }

    /// <summary>Rows moved per batch (oldest first).</summary>
    int BatchSize { get; }

    /// <summary>How long inbox/outbox messaging rows are kept, in days.</summary>
    int MessageRetentionDays { get; }

    /// <summary>How long business records are kept, in days.</summary>
    int BusinessRetentionDays { get; }
}