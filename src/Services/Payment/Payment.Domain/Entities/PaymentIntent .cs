using BuildingBlocks.Shared.Aggregate;
using Payment.Domain.DomainEvents;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Entities;

public class PaymentIntent : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public IdempotencyKey IdempotencyKey { get; private set; } = default!;
    public PaymentStatus Status { get; private set; } = default!;
    public PaymentMethod PaymentMethod { get; private set; } = default!;
    public CardDetails? CardDetails { get; private set; }
    public AuthorizationCode? AuthorizationCode { get; private set; }
    public GatewayReference? GatewayReference { get; private set; }
    public IReadOnlyList<Transaction> Transactions => _transactions.AsReadOnly();
    private readonly List<Transaction> _transactions = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private PaymentIntent() : base() { }

    public PaymentIntent(
        Guid id,
        Guid merchantId,
        Money amount,
        IdempotencyKey idempotencyKey,
        PaymentMethod paymentMethod,
        CardDetails? cardDetails = null) : base(id)
    {
        MerchantId = merchantId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        IdempotencyKey = idempotencyKey ?? throw new ArgumentNullException(nameof(idempotencyKey));
        PaymentMethod = paymentMethod ?? throw new ArgumentNullException(nameof(paymentMethod));
        CardDetails = cardDetails;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentIntentCreatedDomainEvent(Id, MerchantId, Amount, IdempotencyKey.Value));
    }

    public void Authorize(AuthorizationCode authorizationCode, GatewayReference gatewayReference)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot authorize payment in '{Status}' status.");

        AuthorizationCode = authorizationCode;
        GatewayReference = gatewayReference;
        Status = PaymentStatus.Authorized;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new Transaction(TransactionType.Authorization, Amount, gatewayReference);
        _transactions.Add(transaction);

        AddDomainEvent(new PaymentAuthorizedDomainEvent(Id, authorizationCode.Value, Amount, gatewayReference.Value));
    }

    public void Capture(Money? amount = null)
    {
        if (Status != PaymentStatus.Authorized && Status != PaymentStatus.PartiallyCaptured)
            throw new InvalidOperationException($"Cannot capture payment in '{Status}' status.");

        var captureAmount = amount ?? Amount; 

        var capturedSoFar = _transactions
            .Where(t => t.Type == TransactionType.Capture)
            .Sum(t => t.Amount.Amount);

        var remaining = Amount.Amount - capturedSoFar;
        if (captureAmount.Amount > remaining)
            throw new InvalidOperationException($"Capture amount {captureAmount.Amount} exceeds remaining authorized amount {remaining}.");

        if (captureAmount.Currency != Amount.Currency)
            throw new InvalidOperationException("Capture currency must match the original payment currency.");

        var transaction = new Transaction(TransactionType.Capture, captureAmount);
        _transactions.Add(transaction);

        var newCapturedTotal = capturedSoFar + captureAmount.Amount;
        if (newCapturedTotal >= Amount.Amount)
            Status = PaymentStatus.Captured;
        else
            Status = PaymentStatus.PartiallyCaptured;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentCapturedDomainEvent(Id, transaction.Id, captureAmount));
    }

    public void Void()
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot void payment in '{Status}' status.");

        Status = PaymentStatus.Voided;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new Transaction(TransactionType.Void, Amount);
        _transactions.Add(transaction);

        AddDomainEvent(new PaymentVoidedDomainEvent(Id));
    }

    public void Refund(Money? amount = null)
    {
        if (Status != PaymentStatus.Captured && Status != PaymentStatus.PartiallyCaptured && Status != PaymentStatus.PartiallyRefunded)
            throw new InvalidOperationException($"Cannot refund payment in '{Status}' status.");

        var refundAmount = amount ?? new Money(GetTotalCaptured() - GetTotalRefunded(), Amount.Currency);

        var totalCaptured = GetTotalCaptured();
        var totalRefunded = GetTotalRefunded();
        var availableToRefund = totalCaptured - totalRefunded;

        if (refundAmount.Amount > availableToRefund)
            throw new InvalidOperationException($"Refund amount {refundAmount.Amount} exceeds available refund amount {availableToRefund}.");

        if (refundAmount.Currency != Amount.Currency)
            throw new InvalidOperationException("Refund currency must match the original payment currency.");

        var transaction = new Transaction(TransactionType.Refund, refundAmount);
        _transactions.Add(transaction);

        var newRefundedTotal = totalRefunded + refundAmount.Amount;
        if (newRefundedTotal >= totalCaptured)
            Status = PaymentStatus.FullyRefunded;
        else
            Status = PaymentStatus.PartiallyRefunded;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentRefundedDomainEvent(Id, transaction.Id, refundAmount));
    }

    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment in '{Status}' status.");

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    private decimal GetTotalCaptured() =>
        _transactions.Where(t => t.Type == TransactionType.Capture).Sum(t => t.Amount.Amount);

    private decimal GetTotalRefunded() =>
        _transactions.Where(t => t.Type == TransactionType.Refund).Sum(t => t.Amount.Amount);
}