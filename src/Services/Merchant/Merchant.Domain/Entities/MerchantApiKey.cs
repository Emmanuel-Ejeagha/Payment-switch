namespace Merchant.Domain.Entities;

public class MerchantApiKey
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public string KeyHash { get; private set; } = default!;
    public string KeyPrefix { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private MerchantApiKey() { }

    public MerchantApiKey(Guid id, Guid merchantId, string keyHash, string keyPrefix, string environment)
    {
        Id = id;
        MerchantId = merchantId;
        KeyHash = keyHash ?? throw new ArgumentNullException(nameof(keyHash));
        KeyPrefix = keyPrefix ?? throw new ArgumentNullException(nameof(keyPrefix));
        Environment = environment ?? throw new ArgumentNullException(nameof(environment));
        CreatedAt = DateTime.UtcNow;
    }

    public void Revoke()
    {
        if (RevokedAt.HasValue)
            throw new InvalidOperationException("API key is already revoked.");
        RevokedAt = DateTime.UtcNow;
    }
}
