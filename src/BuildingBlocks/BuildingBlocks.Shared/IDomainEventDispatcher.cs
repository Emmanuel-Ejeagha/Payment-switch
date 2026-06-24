namespace BuildingBlocks.Shared;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<DomainEvent> domainEvents, CancellationToken cancellationToken = default);
}