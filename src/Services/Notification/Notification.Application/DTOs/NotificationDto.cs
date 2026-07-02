namespace Notification.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Recipient,
    string Channel,
    string? Subject,
    string? Body,
    string? WebhookUrl,
    string Status,
    int RetryCount,
    DateTime? NextRetryAt,
    DateTime CreatedAt,
    DateTime? ProcessedAt
);