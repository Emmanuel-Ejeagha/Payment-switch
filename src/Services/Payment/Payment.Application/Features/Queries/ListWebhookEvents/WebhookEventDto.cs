namespace Payment.Application.Features.Queries.ListWebhookEvents;

public record WebhookEventDto(
    Guid Id,
    string EventType,
    string Status,
    int Attempts,
    DateTime CreatedAt,
    DateTime? LastAttemptAt,
    string? LastError,
    string? CorrelationId);
