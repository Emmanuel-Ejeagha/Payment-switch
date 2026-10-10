using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class TokenValue : ValueObject
{
    public string Value { get; }
    public DateTime ExpiresAt { get; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? ReplacedByHash { get; private set; }

    public TokenValue(string value, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Token value cannot be empty.", nameof(value));
        Value = value;
        ExpiresAt = expiresAt;
        IsRevoked = false;
    }

    public void Revoke() => Revoke(null);

    public void Revoke(string? replacedByHash)
    {
        IsRevoked = true;
        RevokedAtUtc ??= DateTime.UtcNow;
        ReplacedByHash ??= replacedByHash;
    }

    /// <summary>
    /// Rotation linkage for concurrent-refresh tolerance: the old token points
    /// at its replacement so a legitimately raced (just-rotated) token can be
    /// recognized instead of treated as theft.
    /// </summary>
    public void RotateTo(string newTokenHash)
    {
        Revoke(newTokenHash);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}