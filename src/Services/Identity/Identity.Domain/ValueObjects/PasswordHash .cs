using BuildingBlocks.Shared;

namespace Identity.Domain.ValueObjects;

public class PasswordHash : ValueObject
{
    public string Hash { get; }

    public PasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Hash cannot be empty.", nameof(hash));
        Hash = hash;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Hash;
    }
}
