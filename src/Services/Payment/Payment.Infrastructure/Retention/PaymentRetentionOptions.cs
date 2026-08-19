using BuildingBlocks.Shared.Retention;

namespace Payment.Infrastructure.Retention;

public class PaymentRetentionOptions : IRetentionOptions
{
    public const string SectionName = "Retention";

    public int CleanupIntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int MessageRetentionDays { get; set; } = 7;

    /// <summary>Payment intents/transactions are financial records; keep 7 years (2557 days).</summary>
    public int BusinessRetentionDays { get; set; } = 2557;
}