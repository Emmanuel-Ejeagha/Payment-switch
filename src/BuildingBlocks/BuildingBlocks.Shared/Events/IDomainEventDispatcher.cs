namespace BuildingBlocks.Shared.Events;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}