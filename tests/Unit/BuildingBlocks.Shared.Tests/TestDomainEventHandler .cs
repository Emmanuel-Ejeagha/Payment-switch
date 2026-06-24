using Microsoft.Extensions.DependencyInjection;
using BuildingBlocks.Shared.Events;

namespace BuildingBlocks.Shared.Tests;

// Test domain event
public record TestDomainEvent(string Message) : DomainEvent;

// Spy handler that records invocations
public class TestDomainEventHandler : IDomainEventHandler<TestDomainEvent>
{
    public List<TestDomainEvent> HandledEvents { get; } = new();
    public int HandleCallCount => HandledEvents.Count;

    public Task Handle(TestDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        HandledEvents.Add(domainEvent);
        return Task.CompletedTask;
    }
}

public class DomainEventDispatcherTests
{
    private ServiceProvider CreateServiceProvider(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        configure(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Dispatch_WithRegisteredHandler_ShouldInvokeHandler()
    {
        // Arrange
        var provider = CreateServiceProvider(sc =>
            sc.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>());
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent("Hello");

        // Act
        await dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        var handler = (TestDomainEventHandler)provider.GetRequiredService<IDomainEventHandler<TestDomainEvent>>();
        Assert.Equal(1, handler.HandleCallCount);
        Assert.Equal("Hello", handler.HandledEvents[0].Message);
    }

    [Fact]
    public async Task Dispatch_WithMultipleHandlers_ShouldInvokeAll()
    {
        // Arrange
        var provider = CreateServiceProvider(sc =>
        {
            sc.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>();
            sc.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>(); // register twice
        });
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent("Multi");

        // Act
        await dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        var handlers = provider.GetServices<IDomainEventHandler<TestDomainEvent>>().ToList();
        Assert.Equal(2, handlers.Count);
        Assert.All(handlers, h => Assert.Equal(1, ((TestDomainEventHandler)h).HandleCallCount));
    }

    [Fact]
    public async Task Dispatch_WithNoHandlers_ShouldNotThrow()
    {
        // Arrange
        var provider = CreateServiceProvider(_ => { }); // no handlers registered
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent("None");

        // Act & Assert no exception
        await dispatcher.DispatchAsync(new[] { domainEvent });
    }

    [Fact]
    public async Task Dispatch_ShouldPassCorrectEventToHandler()
    {
        // Arrange
        var provider = CreateServiceProvider(sc =>
            sc.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>());
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent("Payload");

        // Act
        await dispatcher.DispatchAsync(new[] { domainEvent });

        // Assert
        var handler = (TestDomainEventHandler)provider.GetRequiredService<IDomainEventHandler<TestDomainEvent>>();
        var handled = handler.HandledEvents.Single();
        Assert.Same(domainEvent, handled);
    }

    [Fact]
    public async Task Dispatch_ShouldSupportCancellationToken()
    {
        // Arrange
        var provider = CreateServiceProvider(sc =>
            sc.AddScoped<IDomainEventHandler<TestDomainEvent>, TestDomainEventHandler>());
        var dispatcher = provider.GetRequiredService<IDomainEventDispatcher>();
        var domainEvent = new TestDomainEvent("Cancel");
        using var cts = new CancellationTokenSource();
        cts.Cancel(); 

        // Act & Assert
        await dispatcher.DispatchAsync(new[] { domainEvent }, cts.Token);

    }
}