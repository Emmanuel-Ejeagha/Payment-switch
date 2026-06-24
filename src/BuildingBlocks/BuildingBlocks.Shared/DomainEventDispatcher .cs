using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public DomainEventDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task DispatchAsync(IReadOnlyList<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<DomainEvent>.Handle))!;
                var task = (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                await task;
            }
        }
    }
}