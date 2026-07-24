using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class GatewayReference : ValueObject
{
    public string Value { get; } = default!;

    public GatewayReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Gateway reference cannot be empty.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}