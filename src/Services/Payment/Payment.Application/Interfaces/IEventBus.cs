namespace Payment.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}