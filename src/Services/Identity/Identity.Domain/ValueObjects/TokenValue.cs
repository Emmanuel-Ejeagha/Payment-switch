using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class TokenValue : ValueObject
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }
    public bool IsRevoked { get; private set; }

    public TokenValue(string value, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Token value cannot be empty.", nameof(value));
        Value = value;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public void Revoke() => IsRevoked = true;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}