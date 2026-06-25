using BuildingBlocks.Shared.Aggregate;

namespace Identity.Domain.Entities;

public class ApiKey : BaseEntity
{
    public string KeyHash { get; private set; } = default!;
    public string Environment { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private ApiKey() : base() { }

    public ApiKey(Guid id, string keyHash, string environment) : base(id)
    {
        KeyHash = keyHash;
        Environment = environment;
        CreatedAt = DateTime.UtcNow;
        RevokedAt = null;
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
