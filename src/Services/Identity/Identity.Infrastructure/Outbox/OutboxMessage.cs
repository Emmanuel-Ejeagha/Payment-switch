namespace Identity.Infrastructure.Outbox;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime OccurredOn { get; private set; }
    public bool Processed { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? TraceParent { get; private set; }
    public Guid? LeaseToken { get; private set; }
    public DateTime? LeaseExpiresAt { get; private set; }

    private OutboxMessage() { }

    public OutboxMessage(string eventType, string payload, string? correlationId = null, string? traceParent = null)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        OccurredOn = DateTime.UtcNow;
        Processed = false;
        CorrelationId = correlationId;
        TraceParent = traceParent;
    }

    public void AcquireLease(Guid token, DateTime expiresAt)
    {
        LeaseToken = token;
        LeaseExpiresAt = expiresAt;
    }

    public void MarkAsProcessed()
    {
        Processed = true;
        LeaseToken = null;
        LeaseExpiresAt = null;
    }

    public void ReleaseLease()
    {
        LeaseToken = null;
        LeaseExpiresAt = null;
    }
}
