using BuildingBlocks.Shared;

namespace Ledger.Domain.ValueObjects;

public class CorrelationId : ValueObject
{
    public string Value { get; }

    public CorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("CorrelationId cannot be empty.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}