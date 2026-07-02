namespace Notification.Application.Features.Queries.ListNotifications;

public record ListNotificationsQuery(
    string? Recipient,
    string? Channel,
    string? Status,
    int Skip,
    int Take
);
