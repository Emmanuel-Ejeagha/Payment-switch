using BuildingBlocks.Shared.Aggregate;

namespace Payment.Domain.Entities;

public class WebhookEvent : AggregateRoot
{
    public const string StatusPending = "Pending";
    public const string StatusSucceeded = "Succeeded";
    public const string StatusFailed = "Failed";

    public Guid MerchantId { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public string Status { get; private set; } = StatusPending;
    public int Attempts { get; private set; }
    public DateTime? NextAttemptAt { get; private set; }
    public DateTime? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private WebhookEvent() : base() { }

    public WebhookEvent(
        Guid id,
        Guid merchantId,
        string eventType,
        string payload,
        string? correlationId = null) : base(id)
    {
        MerchantId = merchantId;
        EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        Status = StatusPending;
        Attempts = 0;
        NextAttemptAt = DateTime.UtcNow;
        CorrelationId = correlationId;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkSucceeded()
    {
        Status = StatusSucceeded;
        LastAttemptAt = DateTime.UtcNow;
        NextAttemptAt = null;
        LastError = null;
    }

    public void MarkFailed(string? error, TimeSpan retryAfter)
    {
        Status = StatusFailed;
        Attempts++;
        LastAttemptAt = DateTime.UtcNow;
        NextAttemptAt = DateTime.UtcNow.Add(retryAfter);
        LastError = error;
    }

    public void ResetForReplay()
    {
        Status = StatusPending;
        Attempts = 0;
        NextAttemptAt = DateTime.UtcNow;
        LastError = null;
        LastAttemptAt = null;
    }
}
