namespace Ledger.Infrastructure.Inbox;

public enum InboxState
{
    Processing = 0,
    Processed = 1,
    /// <summary>
    /// A business failure was recorded (handler returned failure). The row
    /// stays eligible for redelivery-driven retry; <see cref="Attempts"/> and
    /// <see cref="LastError"/> keep the audit trail. Poison messages still end
    /// in the DLQ via the header retry budget.
    /// </summary>
    Failed = 2
}

public class InboxMessage
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = null!;
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime OccurredOn { get; private set; }
    public InboxState State { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    private InboxMessage() { }

    public InboxMessage(string messageId, string eventType, string payload)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        EventType = eventType;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
        State = InboxState.Processing;
    }

    public void Reclaim()
    {
        State = InboxState.Processing;
        ProcessedAt = null;
    }

    public void MarkAsProcessed()
    {
        State = InboxState.Processed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string error)
    {
        State = InboxState.Failed;
        Attempts++;
        LastError = error.Length > 2000 ? error[..2000] : error;
    }
}