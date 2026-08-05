using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

/// <summary>
/// A card security code (CVC/CVV/CID) held only for the lifetime of a single
/// authorization request.
///
/// PCI DSS 3.2 forbids storing this value after authorization, so this type is
/// deliberately NOT part of <see cref="CardDetails"/> — <c>CardDetails</c> is an
/// EF-owned type that gets persisted with the payment intent. This one is passed
/// as a standalone argument down to the gateway provider and then dropped.
///
/// <see cref="ToString"/> is overridden to redact the value so it cannot leak into
/// logs, exception messages, or serialized diagnostics by accident.
/// </summary>
public sealed class CardSecurityCode : ValueObject
{
    public string Value { get; }

    public CardSecurityCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Security code is required.", nameof(value));

        var trimmed = value.Trim();

        // Amex uses a 4-digit CID; everything else is a 3-digit CVC/CVV.
        if (trimmed.Length is < 3 or > 4 || !trimmed.All(char.IsAsciiDigit))
            throw new ArgumentException("Security code must be 3 or 4 digits.", nameof(value));

        Value = trimmed;
    }

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length is >= 3 and <= 4
        && value.Trim().All(char.IsAsciiDigit);

    /// <summary>Always redacted — never returns the code itself.</summary>
    public override string ToString() => "***";

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
