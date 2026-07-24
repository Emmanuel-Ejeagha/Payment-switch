using BuildingBlocks.Shared;

namespace Ledger.Domain.ValueObjects;

public class Currency : ValueObject
{
    public string Code { get; }

    public Currency(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != 3)
            throw new ArgumentException("Currency code must be a 3-letter ISO code.", nameof(code));
        Code = code.ToUpperInvariant();
    }

    public static readonly Currency USD = new("USD");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
    }

    public static implicit operator string(Currency c) => c.Code;
    public override string ToString() => Code;
}