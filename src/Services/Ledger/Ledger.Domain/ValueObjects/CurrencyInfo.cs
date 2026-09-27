using BuildingBlocks.Shared;

namespace Ledger.Domain.ValueObjects;

/// <summary>
/// ISO 4217 currency metadata: the minor-unit exponent (digits after the decimal
/// separator) used to convert between major and minor units. Most currencies are
/// 2-decimal; a handful use 0 (JPY, KRW, CLP, ISK, VND) or 3 (KWD, BHD, OMR, JOD,
/// TND, LYD, IQD). Unknown codes fall back to the ISO 4217 default of 2 so a new
/// currency never silently mis-scales.
/// </summary>
public class CurrencyInfo : ValueObject
{
    private const int DefaultExponent = 2;

    private static readonly IReadOnlyDictionary<string, int> Exponents =
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

    public string Code { get; }
    public int MinorUnitsExponent { get; }

    private CurrencyInfo(string code, int minorUnitsExponent)
    {
        Code = code;
        MinorUnitsExponent = minorUnitsExponent;
    }

    public static CurrencyInfo Lookup(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO code.", nameof(code));

        var normalized = code.ToUpperInvariant();
        return Exponents.TryGetValue(normalized, out var exponent)
            ? new CurrencyInfo(normalized, exponent)
            : new CurrencyInfo(normalized, DefaultExponent);
    }

    /// <summary>
    /// Converts a minor-unit amount to major units using this currency's exponent,
    /// without floating-point drift (integer scale factors only).
    /// </summary>
    public decimal ToMajorUnits(long minorUnits)
    {
        var scale = Scale(MinorUnitsExponent);
        return minorUnits / scale;
    }

    /// <summary>
    /// Converts a major-unit amount to minor units, rounding half away from zero so
    /// 0.125 KWD lands on an exact 125 fils rather than banker's-rounding to 12.
    /// </summary>
    public long ToMinorUnits(decimal majorUnits)
    {
        var scaled = majorUnits * Scale(MinorUnitsExponent);
        return checked((long)Math.Round(scaled, MidpointRounding.AwayFromZero));
    }

    private static decimal Scale(int exponent) => exponent switch
    {
        0 => 1m,
        1 => 10m,
        2 => 100m,
        3 => 1000m,
        _ => throw new ArgumentOutOfRangeException(nameof(exponent))
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return MinorUnitsExponent;
    }

    public override string ToString() => $"{Code} (exponent {MinorUnitsExponent})";
}
