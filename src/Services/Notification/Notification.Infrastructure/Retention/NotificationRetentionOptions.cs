using BuildingBlocks.Shared.Retention;

namespace Notification.Infrastructure.Retention;

public class NotificationRetentionOptions : IRetentionOptions
{
    public const string SectionName = "Retention";

    public int CleanupIntervalMinutes { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public int MessageRetentionDays { get; set; } = 7;

    /// <summary>Delivered notifications are operational history; keep 90 days.</summary>
    public int BusinessRetentionDays { get; set; } = 90;
}