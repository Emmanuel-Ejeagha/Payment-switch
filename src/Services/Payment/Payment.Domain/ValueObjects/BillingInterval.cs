using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

/// <summary>
/// Billing cadence for a plan, expressed as a unit plus a multiplier
/// (e.g. Month x 3 for quarterly billing).
/// </summary>
public class BillingInterval : ValueObject
{
    public string Unit { get; }
    public int Count { get; }

    public BillingInterval(string unit, int count = 1)
    {
        if (string.IsNullOrWhiteSpace(unit))
            throw new ArgumentException("Interval unit is required.", nameof(unit));

        var normalized = unit.Trim().ToLowerInvariant();
        if (!AllowedUnits.Contains(normalized))
            throw new ArgumentException($"Interval unit must be one of: {string.Join(", ", AllowedUnits)}.", nameof(unit));

        if (count < 1)
            throw new ArgumentException("Interval count must be at least 1.", nameof(count));

        Unit = normalized;
        Count = count;
    }

    public static readonly IReadOnlySet<string> AllowedUnits =
        new HashSet<string> { "day", "week", "month", "year" };

    /// <summary>Advances a period start by exactly one billing cycle.</summary>
    public DateTime AddTo(DateTime from) => Unit switch
    {
        "day" => from.AddDays(Count),
        "week" => from.AddDays(7 * Count),
        "month" => from.AddMonths(Count),
        "year" => from.AddYears(Count),
        _ => throw new InvalidOperationException($"Unhandled interval unit '{Unit}'.")
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Unit;
        yield return Count;
    }

    public override string ToString() => Count == 1 ? Unit : $"{Count} {Unit}s";
}
