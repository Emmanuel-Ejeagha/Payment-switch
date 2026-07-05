using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

public class WebhookUrl : ValueObject
{
    public string Value { get; }

    public WebhookUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Webhook URL cannot be empty.", nameof(value));
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Webhook URL must be a valid absolute HTTP/HTTPS URL.", nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}