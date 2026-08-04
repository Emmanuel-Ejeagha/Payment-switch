using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class PaymentLink : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public string Code { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentLink() : base() { }

    public PaymentLink(
        Guid id,
        Guid merchantId,
        Money amount,
        string code,
        string description) : base(id)
    {
        MerchantId = merchantId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Description = description ?? string.Empty;
        Active = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Active = false;
    }
}
