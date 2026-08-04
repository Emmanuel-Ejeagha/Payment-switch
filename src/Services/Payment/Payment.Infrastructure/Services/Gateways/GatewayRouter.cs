using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public class GatewayRouter
{
    private const long HighValueMinorThreshold = 500_00;

    public IReadOnlyList<GatewayProviderEntry> Resolve(Money amount, CardDetails? cardDetails, IReadOnlyCollection<GatewayProviderEntry> providers)
    {
        if (providers.Count == 0)
            return Array.Empty<GatewayProviderEntry>();

        var preferred = SelectPreferredProvider(amount, cardDetails);
        var primary = providers.FirstOrDefault(p => string.Equals(p.Provider.Name, preferred, StringComparison.OrdinalIgnoreCase));
        var ordered = new List<GatewayProviderEntry>();

        if (primary is not null)
            ordered.Add(primary);

        ordered.AddRange(providers.Where(p => p != primary));
        return ordered;
    }

    private static string SelectPreferredProvider(Money amount, CardDetails? cardDetails)
    {
        if (amount.Amount >= HighValueMinorThreshold)
            return "stripe";

        var brand = cardDetails?.Brand?.ToLowerInvariant();
        if (brand is not null && (brand.Contains("visa") || brand.Contains("master")))
            return "stripe";

        return "paystack";
    }
}
