namespace Merchant.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }
    public bool Processed { get; private set; }
    public string? CorrelationId { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string eventType, string payload, string? correlationId = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
        Processed = false;
        CorrelationId = correlationId;
    }

    public void MarkAsProcessed()
    {
        Processed = true;
    }
}