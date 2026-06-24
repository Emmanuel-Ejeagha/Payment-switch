namespace BuildingBlocks.Shared;

public interface IDomainEventHandler<TEvent> where TEvent : DomainEvent
{
    Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}