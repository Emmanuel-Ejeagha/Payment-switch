namespace Notification.Application.Features.Commands.CreateNotification;

public record CreateNotificationCommand(
    string Recipient,
    string Channel,
    string? Subject,
    string? Body,
    string? WebhookUrl,
    string Payload,
    int? MaxRetries = 5
);
