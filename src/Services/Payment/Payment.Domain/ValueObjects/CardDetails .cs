using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class CardDetails : ValueObject
{
    public string LastFour { get; }
    public string Brand { get; }
    public string? Token { get; }

    public CardDetails(string lastFour, string brand, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(lastFour) || lastFour.Length != 4)
            throw new ArgumentException("Last four must be 4 digits.", nameof(lastFour));
        if (string.IsNullOrWhiteSpace(brand))
            throw new ArgumentException("Brand is required.", nameof(brand));

        LastFour = lastFour;
        Brand = brand;
        Token = token;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return LastFour;
        yield return Brand;
        yield return Token;
    }
}