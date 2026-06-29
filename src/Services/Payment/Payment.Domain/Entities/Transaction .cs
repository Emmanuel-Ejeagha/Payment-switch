using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class Transaction : BaseEntity
{
    public TransactionType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public GatewayReference? GatewayReference { get; private set; }
    public DateTime Timestamp { get; private set; }

    private Transaction() : base() { }

    public Transaction(TransactionType type, Money amount, GatewayReference? gatewayReference = null) : base()
    {
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        GatewayReference = gatewayReference;
        Timestamp = DateTime.UtcNow;
    }
}
