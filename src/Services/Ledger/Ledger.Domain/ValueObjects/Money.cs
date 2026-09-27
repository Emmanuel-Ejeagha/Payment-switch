using BuildingBlocks.Shared;

namespace Ledger.Domain.ValueObjects;

public class Money : ValueObject
{
    public long Amount { get; }
    public string Currency { get; }

    /// <summary>
    /// Digits after the decimal separator for this currency (2 for USD, 0 for JPY,
    /// 3 for KWD, ...). Explicit rather than a hardcoded 2-decimal assumption.
    /// </summary>
    public int MinorUnitsExponent => CurrencyInfo.Lookup(Currency).MinorUnitsExponent;

    public Money(long amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a valid 3-letter ISO code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    /// <summary>
    /// Major-unit (decimal) representation derived from the currency's exponent.
    /// </summary>
    public decimal ToMajorAmount() => CurrencyInfo.Lookup(Currency).ToMajorUnits(Amount);

    /// <summary>
    /// Builds a <see cref="Money"/> from a major-unit amount, converting through the
    /// currency's exponent (rounding half away from zero). Throws on negative values.
    /// </summary>
    public static Money FromMajorAmount(decimal majorAmount, string currency)
    {
        var minorUnits = CurrencyInfo.Lookup(currency).ToMinorUnits(majorAmount);
        return new Money(minorUnits, currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}