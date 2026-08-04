using BuildingBlocks.Shared.Aggregate;

namespace Payment.Domain.Entities;

public class CardToken : BaseEntity
{
    public Guid MerchantId { get; private set; }
    public string Token { get; private set; } = default!;
    public string LastFour { get; private set; } = default!;
    public string Brand { get; private set; } = default!;
    public int ExpiryMonth { get; private set; }
    public int ExpiryYear { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CardToken() : base() { }

    public CardToken(
        Guid merchantId,
        string token,
        string lastFour,
        string brand,
        int expiryMonth,
        int expiryYear) : base()
    {
        MerchantId = merchantId;
        Token = token ?? throw new ArgumentNullException(nameof(token));
        LastFour = lastFour ?? throw new ArgumentNullException(nameof(lastFour));
        Brand = brand ?? throw new ArgumentNullException(nameof(brand));
        ExpiryMonth = expiryMonth;
        ExpiryYear = expiryYear;
        CreatedAt = DateTime.UtcNow;
    }
}
