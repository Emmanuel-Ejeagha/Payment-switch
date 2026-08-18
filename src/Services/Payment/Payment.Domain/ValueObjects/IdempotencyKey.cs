using BuildingBlocks.Shared;
using System.Text.RegularExpressions;

namespace Payment.Domain.ValueObjects;

public class IdempotencyKey : ValueObject
{
    /// <summary>
    /// Upper bound matching the transactional <c>IdempotencyKey</c> column
    /// (<c>HasMaxLength(200)</c>) so a key the API accepts always fits the store.
    /// </summary>
    public const int MaxLength = 200;

    private static readonly Regex ValidCharacters = new("^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    public string Value { get; }

    public IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(value));
        Value = value;
    }

    /// <summary>
    /// Validates a client-supplied key without constructing the value object:
    /// non-empty, within <see cref="MaxLength"/>, and ASCII letters/digits/'-'/'_'.
    /// </summary>
    public static bool IsWellFormed(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && ValidCharacters.IsMatch(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}