using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class Plan : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public BillingInterval Interval { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Plan() : base() { }

    public Plan(
        Guid id,
        Guid merchantId,
        string code,
        string name,
        Money amount,
        BillingInterval interval,
        string? description = null) : base(id)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("MerchantId is required.", nameof(merchantId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plan name is required.", nameof(name));
        if (amount is null || amount.Amount <= 0)
            throw new ArgumentException("Plan amount must be greater than zero.", nameof(amount));

        MerchantId = merchantId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Name = name;
        Amount = amount;
        Interval = interval ?? throw new ArgumentNullException(nameof(interval));
        Description = description;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (!Active) return;

        Active = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NewCode() => $"plan_{Guid.NewGuid().ToString("N")[..24]}";
}
