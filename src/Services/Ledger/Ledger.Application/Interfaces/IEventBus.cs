namespace Ledger.Application.Interfaces;

public interface IEventBus
{
    Task PublishAsync(string eventType, string payload, string? messageId = null, string? correlationId = null, CancellationToken cancellationToken = default);
}