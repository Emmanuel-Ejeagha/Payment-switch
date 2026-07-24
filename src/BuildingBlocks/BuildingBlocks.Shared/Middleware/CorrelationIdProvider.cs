using System.Threading;

namespace BuildingBlocks.Shared.Middleware;

public interface ICorrelationIdProvider
{
    string CorrelationId { get; }
}

public class CorrelationIdProvider : ICorrelationIdProvider
{
    private static readonly AsyncLocal<string> _correlationId = new();

    public string CorrelationId => _correlationId.Value ?? string.Empty;

    public void Set(string correlationId)
    {
        _correlationId.Value = correlationId;
    }
}
