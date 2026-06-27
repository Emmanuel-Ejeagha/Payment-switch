using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

public class BusinessName : ValueObject
{
    public string Value { get; }

    public BusinessName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Business name cannot be empty.", nameof(value));
        if (value.Length < 2 || value.Length > 100)
            throw new ArgumentException("Business name must be between 2 and 100 characters.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}