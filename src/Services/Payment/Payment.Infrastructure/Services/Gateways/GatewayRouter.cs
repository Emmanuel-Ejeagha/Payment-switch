using BuildingBlocks.Shared.ValueObjects;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public class GatewayRouter
{
    // Compared in MAJOR units: raw minor-unit comparison misroutes 3-decimal
    // currencies (KWD 100.000 = 100_000 minor looks >= 500_00 minor).
    private const decimal HighValueMajorThreshold = 500m;

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
        var major = CurrencyInfo.Lookup(amount.Currency).ToMajorUnits(amount.Amount);
        if (major >= HighValueMajorThreshold)
            return "stripe";

        var brand = cardDetails?.Brand?.ToLowerInvariant();
        if (brand is not null && (brand.Contains("visa") || brand.Contains("master")))
            return "stripe";

        return "paystack";
    }
}
