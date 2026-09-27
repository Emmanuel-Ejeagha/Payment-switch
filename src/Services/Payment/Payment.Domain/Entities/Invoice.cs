using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.DomainEvents;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class Invoice : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public string Code { get; private set; } = default!;
    public Money Amount { get; private set; } = default!;
    public InvoiceStatus Status { get; private set; } = default!;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    /// <summary>Set once a charge has been attempted for this invoice.</summary>
    public Guid? PaymentIntentId { get; private set; }
    public int AttemptCount { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private Invoice() : base() { }

    public Invoice(
        Guid id,
        Guid merchantId,
        Guid customerId,
        Guid subscriptionId,
        string code,
        Money amount,
        DateTime periodStart,
        DateTime periodEnd) : base(id)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("MerchantId is required.", nameof(merchantId));
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (subscriptionId == Guid.Empty)
            throw new ArgumentException("SubscriptionId is required.", nameof(subscriptionId));
        if (amount is null || amount.Amount <= 0)
            throw new ArgumentException("Invoice amount must be greater than zero.", nameof(amount));
        if (periodEnd <= periodStart)
            throw new ArgumentException("Period end must be after period start.", nameof(periodEnd));

        MerchantId = merchantId;
        CustomerId = customerId;
        SubscriptionId = subscriptionId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Amount = amount;
        Status = InvoiceStatus.Open;
        PeriodStart = periodStart;
        PeriodEnd = periodEnd;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new InvoiceIssuedDomainEvent(Id, MerchantId, CustomerId, SubscriptionId, Amount, Code));
    }

    public void MarkPaid(Guid paymentIntentId)
    {
        if (Status == InvoiceStatus.Paid) return;
        if (Status != InvoiceStatus.Open)
            throw new InvalidOperationException($"Cannot pay an invoice in '{Status}' status.");

        PaymentIntentId = paymentIntentId;
        Status = InvoiceStatus.Paid;
        AttemptCount++;
        LastError = null;
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new InvoicePaidDomainEvent(Id, MerchantId, CustomerId, SubscriptionId, Amount, paymentIntentId));
    }

    /// <summary>Records a failed collection attempt; the invoice stays open for retry.</summary>
    public void RecordFailedAttempt(string? error, Guid? paymentIntentId = null)
    {
        if (Status != InvoiceStatus.Open)
            throw new InvalidOperationException($"Cannot attempt payment on an invoice in '{Status}' status.");

        AttemptCount++;
        LastError = error;
        if (paymentIntentId.HasValue) PaymentIntentId = paymentIntentId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Gives up on collection after retries are exhausted.</summary>
    public void MarkUncollectible()
    {
        if (Status != InvoiceStatus.Open)
            throw new InvalidOperationException($"Cannot abandon an invoice in '{Status}' status.");

        Status = InvoiceStatus.Uncollectible;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new InvoiceUncollectibleDomainEvent(Id, MerchantId, CustomerId, SubscriptionId, Amount));
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot void a paid invoice.");
        if (Status == InvoiceStatus.Void) return;

        Status = InvoiceStatus.Void;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NewCode() => $"inv_{Guid.NewGuid().ToString("N")[..24]}";
}
