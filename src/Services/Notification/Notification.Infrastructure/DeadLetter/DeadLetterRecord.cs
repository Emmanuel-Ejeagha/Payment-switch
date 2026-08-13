namespace Notification.Infrastructure.DeadLetter;

/// <summary>
/// A message that exhausted its retries and was routed to the dead-letter queue.
/// Persisted by the DLQ consumer so no dead letter is silently dropped.
/// </summary>
public class DeadLetterRecord
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string Queue { get; private set; } = null!;
    public string Reason { get; private set; } = null!;
    public int RetryCount { get; private set; }
    public string Payload { get; private set; } = null!;
    public DateTime ReceivedAt { get; private set; }

    private DeadLetterRecord() { }

    public DeadLetterRecord(string messageId, string eventType, string queue, string reason, int retryCount, string payload)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        EventType = eventType;
        Queue = queue;
        Reason = reason;
        RetryCount = retryCount;
        Payload = payload;
        ReceivedAt = DateTime.UtcNow;
    }
}