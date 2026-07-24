using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class FullName : ValueObject
{
    public string Value { get; }

    public FullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Full name cannot be empty.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}