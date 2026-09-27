namespace Ledger.Infrastructure.Inbox;

public enum InboxState
{
    Processing = 0,
    Processed = 1
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
}