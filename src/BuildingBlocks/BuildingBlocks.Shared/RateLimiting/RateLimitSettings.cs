namespace BuildingBlocks.Shared.RateLimiting;

/// <summary>
/// Config-driven rate-limit budgets (optional <c>RateLimiting</c> section;
/// defaults preserve the existing budgets). Per-client partitions (see
/// <see cref="RateLimitPartitionKey"/>): each IP / authenticated user gets its
/// own window.
/// </summary>
public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    public int DefaultPermitLimit { get; set; } = 100;
    public int DefaultWindowSeconds { get; set; } = 10;
    public int DefaultQueueLimit { get; set; } = 5;

    public int StrictPermitLimit { get; set; } = 20;
    public int StrictWindowSeconds { get; set; } = 10;
}
