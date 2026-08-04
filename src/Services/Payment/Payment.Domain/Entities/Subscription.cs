using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.DomainEvents;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class Subscription : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid PlanId { get; private set; }
    public string Code { get; private set; } = default!;
    public SubscriptionStatus Status { get; private set; } = default!;
    /// <summary>Vault token charged on each cycle.</summary>
    public string CardToken { get; private set; } = default!;
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    /// <summary>When the next invoice should be generated; null once canceled.</summary>
    public DateTime? NextBillingAt { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }
    public DateTime? CanceledAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }

    private Subscription() : base() { }

    public Subscription(
        Guid id,
        Guid merchantId,
        Guid customerId,
        Guid planId,
        string code,
        string cardToken,
        BillingInterval interval,
        DateTime? startAt = null) : base(id)
    {
        if (merchantId == Guid.Empty)
            throw new ArgumentException("MerchantId is required.", nameof(merchantId));
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId is required.", nameof(customerId));
        if (planId == Guid.Empty)
            throw new ArgumentException("PlanId is required.", nameof(planId));
        if (string.IsNullOrWhiteSpace(cardToken))
            throw new ArgumentException("A card token is required to bill a subscription.", nameof(cardToken));
        ArgumentNullException.ThrowIfNull(interval);

        MerchantId = merchantId;
        CustomerId = customerId;
        PlanId = planId;
        Code = code ?? throw new ArgumentNullException(nameof(code));
        CardToken = cardToken;
        Status = SubscriptionStatus.Incomplete;

        CurrentPeriodStart = startAt ?? DateTime.UtcNow;
        CurrentPeriodEnd = interval.AddTo(CurrentPeriodStart);
        // The first cycle bills immediately; subsequent cycles bill at period end.
        NextBillingAt = CurrentPeriodStart;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new SubscriptionCreatedDomainEvent(Id, MerchantId, CustomerId, PlanId, Code));
    }

    /// <summary>Records a successful collection and rolls the billing period forward.</summary>
    public void MarkCyclePaid(BillingInterval interval)
    {
        ArgumentNullException.ThrowIfNull(interval);

        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Cannot bill a canceled subscription.");

        if (CancelAtPeriodEnd)
        {
            Cancel();
            return;
        }

        CurrentPeriodStart = CurrentPeriodEnd;
        CurrentPeriodEnd = interval.AddTo(CurrentPeriodStart);
        NextBillingAt = CurrentPeriodStart;
        Status = SubscriptionStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new SubscriptionRenewedDomainEvent(Id, MerchantId, CurrentPeriodStart, CurrentPeriodEnd));
    }

    /// <summary>Flags a failed collection and schedules the next retry.</summary>
    public void MarkPastDue(DateTime retryAt)
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Cannot bill a canceled subscription.");

        Status = SubscriptionStatus.PastDue;
        NextBillingAt = retryAt;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new SubscriptionPastDueDomainEvent(Id, MerchantId, retryAt));
    }

    /// <summary>Ends the subscription immediately.</summary>
    public void Cancel()
    {
        if (Status == SubscriptionStatus.Canceled) return;

        Status = SubscriptionStatus.Canceled;
        NextBillingAt = null;
        CanceledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new SubscriptionCanceledDomainEvent(Id, MerchantId, CustomerId, CanceledAt.Value));
    }

    /// <summary>Lets the current period run out, then cancels instead of renewing.</summary>
    public void CancelAtEndOfPeriod()
    {
        if (Status == SubscriptionStatus.Canceled)
            throw new InvalidOperationException("Subscription is already canceled.");

        CancelAtPeriodEnd = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCardToken(string cardToken)
    {
        if (string.IsNullOrWhiteSpace(cardToken))
            throw new ArgumentException("Card token is required.", nameof(cardToken));

        CardToken = cardToken;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NewCode() => $"sub_{Guid.NewGuid().ToString("N")[..24]}";
}
