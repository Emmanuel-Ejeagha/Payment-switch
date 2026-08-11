namespace Notification.Application.DTOs;

public record NotificationPreferenceDto(
    Guid Id,
    string Recipient,
    string Channel,
    string EventType,
    bool Enabled);
