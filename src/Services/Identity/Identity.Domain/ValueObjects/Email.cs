using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class Email : ValueObject
{
    public const int MaxLength = 255;

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        // Normalize once at construction so stored values and duplicate checks
        // can never disagree on case or surrounding whitespace. Format rules
        // stay with the application validators; length matches the column.
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length > MaxLength)
            throw new ArgumentException($"Email must be at most {MaxLength} characters.", nameof(value));

        Value = normalized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;
}
