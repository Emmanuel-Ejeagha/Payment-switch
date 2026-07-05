namespace Settlement.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }
    public bool Processed { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string eventType, string payload)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
        Processed = false;
    }

    public void MarkAsProcessed()
    {
        Processed = true;
    }
}