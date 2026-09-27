using BuildingBlocks.Shared.Resilience;

namespace Payment.Infrastructure.Services.Gateways;

public class GatewayProviderRegistry
{
    private readonly Dictionary<string, GatewayProviderEntry> _providers;

    public GatewayProviderRegistry(IEnumerable<IPaymentGatewayProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Name, p => new GatewayProviderEntry(p, new CircuitBreaker(5, TimeSpan.FromSeconds(30))));
    }

    public IReadOnlyCollection<GatewayProviderEntry> All => _providers.Values;

    public GatewayProviderEntry Get(string name)
    {
        if (!_providers.TryGetValue(name, out var entry))
            throw new KeyNotFoundException($"Payment gateway provider '{name}' is not registered.");
        return entry;
    }
}

public record GatewayProviderEntry(IPaymentGatewayProvider Provider, CircuitBreaker CircuitBreaker);
