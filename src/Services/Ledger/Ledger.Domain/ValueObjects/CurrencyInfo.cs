using BuildingBlocks.Shared;
using BuildingBlocks.Shared.ValueObjects;

namespace Ledger.Domain.ValueObjects;

/// <summary>
/// Ledger-local facade over the shared <see cref="BuildingBlocks.Shared.ValueObjects.CurrencyInfo"/>
/// so existing domain references keep compiling while the exponent table lives once in BuildingBlocks.
/// </summary>
public class CurrencyInfo : ValueObject
{
    private readonly BuildingBlocks.Shared.ValueObjects.CurrencyInfo _inner;

    private CurrencyInfo(BuildingBlocks.Shared.ValueObjects.CurrencyInfo inner)
    {
        _inner = inner;
    }

    public string Code => _inner.Code;
    public int MinorUnitsExponent => _inner.MinorUnitsExponent;

    public static CurrencyInfo Lookup(string code)
    {
        return new CurrencyInfo(BuildingBlocks.Shared.ValueObjects.CurrencyInfo.Lookup(code));
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
