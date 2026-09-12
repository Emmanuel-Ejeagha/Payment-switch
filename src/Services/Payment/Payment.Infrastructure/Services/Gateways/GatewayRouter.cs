using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Services.Gateways;

public class GatewayRouter
{
    private const decimal HighValueMajorThreshold = 500m;

    // Minor-unit exponents per currency. Mirrors Ledger's CurrencyInfo table;
    // Phase 5 Step 5.2 shares that model via BuildingBlocks and removes this copy.
    private static readonly IReadOnlyDictionary<string, int> MinorUnitExponents =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["JPY"] = 0,
            ["KRW"] = 0,
            ["CLP"] = 0,
            ["ISK"] = 0,
            ["VND"] = 0,
            ["KWD"] = 3,
            ["BHD"] = 3,
            ["OMR"] = 3,
            ["JOD"] = 3,
            ["TND"] = 3,
            ["LYD"] = 3,
            ["IQD"] = 3,
        };

    private static bool IsHighValue(Money amount)
    {
        var exponent = MinorUnitExponents.TryGetValue(amount.Currency, out var e) ? e : 2;
        var divisor = exponent switch
        {
            0 => 1m,
            1 => 10m,
            2 => 100m,
            3 => 1000m,
            _ => 100m
        };
        return amount.Amount / divisor >= HighValueMajorThreshold;
    }

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
        // Thresholds compare major units: 500_00 minor is $500 but only ¥500,
        // so raw minor-unit comparison misroutes 0- and 3-decimal currencies.
        if (IsHighValue(amount))
            return "stripe";

        var brand = cardDetails?.Brand?.ToLowerInvariant();
        if (brand is not null && (brand.Contains("visa") || brand.Contains("master")))
            return "stripe";

        return "paystack";
    }
}
