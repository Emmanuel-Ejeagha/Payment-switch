using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class AuthorizationCode : ValueObject
{
    public string Value { get; }

    public AuthorizationCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Authorization code cannot be empty.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}