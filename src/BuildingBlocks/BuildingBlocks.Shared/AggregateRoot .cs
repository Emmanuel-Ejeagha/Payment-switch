namespace BuildingBlocks.Shared;

public abstract class AggregateRoot : BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected AggregateRoot() : base() { }

    protected AggregateRoot(Guid id) : base(id) { }
}