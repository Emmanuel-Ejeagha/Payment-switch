namespace Notification.Infrastructure.Inbox;

public class InboxMessage
{
    public Guid Id { get; private set; }
    public string MessageId { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private InboxMessage() { }

    public InboxMessage(string messageId, string eventType, string payload)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        EventType = eventType;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
    }

    public void MarkAsProcessed()
    {
        ProcessedAt = DateTime.UtcNow;
    }
}