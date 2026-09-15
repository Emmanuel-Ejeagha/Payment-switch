using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class FullName : ValueObject
{
    public string Value { get; }

    public FullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Full name cannot be empty.", nameof(value));
        var sanitized = value.Trim();
        if (sanitized.Length > 100)
            throw new ArgumentException("Full name must not exceed 100 characters.", nameof(value));
        // Strip control characters
        sanitized = new string(sanitized.Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(sanitized))
            throw new ArgumentException("Full name cannot be empty.", nameof(value));
        Value = sanitized;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}