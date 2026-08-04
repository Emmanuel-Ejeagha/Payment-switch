using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

public class WebhookSecret : ValueObject
{
    public string Value { get; }

    public WebhookSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Webhook secret cannot be empty.", nameof(value));
        Value = value;
    }

    public static WebhookSecret Generate()
    {
        var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return new WebhookSecret(Convert.ToHexString(bytes).ToLowerInvariant());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
