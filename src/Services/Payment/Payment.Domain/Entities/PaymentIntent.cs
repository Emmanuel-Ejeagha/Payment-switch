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
    private List<Transaction> _transactions = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public uint RowVersion { get; private set; }
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

    public void Authorize(AuthorizationCode authorizationCode, GatewayReference gatewayReference, string? idempotencyKey = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot authorize payment in '{Status}' status.");

        AuthorizationCode = authorizationCode;
        GatewayReference = gatewayReference;
        Status = PaymentStatus.Authorized;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new Transaction(TransactionType.Authorization, new Money(Amount.Amount, Amount.Currency), new GatewayReference(gatewayReference.Value), idempotencyKey);
        _transactions.Add(transaction);

        AddDomainEvent(new PaymentAuthorizedDomainEvent(Id, MerchantId, authorizationCode.Value, Amount, gatewayReference.Value));
    }

    public void RequireAction(GatewayReference gatewayReference)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot require action for payment in '{Status}' status.");

        GatewayReference = gatewayReference;
        Status = PaymentStatus.RequiresAction;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentRequiresActionDomainEvent(Id, MerchantId, Amount, gatewayReference.Value));
    }

    public void MarkProcessing(string status = "Processing")
    {
        if (Status != PaymentStatus.RequiresAction)
            throw new InvalidOperationException($"Cannot mark '{status}' payment as processing from '{Status}' status.");

        Status = PaymentStatus.Processing;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentProcessingDomainEvent(Id, MerchantId, status));
    }

    public void ConfirmAction(AuthorizationCode authorizationCode, GatewayReference gatewayReference, string? idempotencyKey = null)
    {
        if (Status != PaymentStatus.RequiresAction && Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot confirm payment in '{Status}' status.");

        AuthorizationCode = authorizationCode;
        GatewayReference = gatewayReference;
        Status = PaymentStatus.Authorized;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new Transaction(TransactionType.Authorization, new Money(Amount.Amount, Amount.Currency), new GatewayReference(gatewayReference.Value), idempotencyKey);
        _transactions.Add(transaction);

        AddDomainEvent(new PaymentAuthorizedDomainEvent(Id, MerchantId, authorizationCode.Value, Amount, gatewayReference.Value));
    }

    public void Capture(Money? amount = null, string? idempotencyKey = null)
    {
        if (Status != PaymentStatus.Authorized && Status != PaymentStatus.PartiallyCaptured)
            throw new InvalidOperationException($"Cannot capture payment in '{Status}' status.");

        var captureAmount = amount ?? new Money(Amount.Amount, Amount.Currency);

        var capturedSoFar = _transactions
            .Where(t => t.Type == TransactionType.Capture)
            .Sum(t => t.Amount.Amount);

        var remaining = Amount.Amount - capturedSoFar;
        if (captureAmount.Amount > remaining)
            throw new InvalidOperationException($"Capture amount {captureAmount.Amount} exceeds remaining authorized amount {remaining}.");

        if (captureAmount.Currency != Amount.Currency)
            throw new InvalidOperationException("Capture currency must match the original payment currency.");

        var transaction = new Transaction(TransactionType.Capture, captureAmount, idempotencyKey: idempotencyKey);
        _transactions.Add(transaction);

        var newCapturedTotal = capturedSoFar + captureAmount.Amount;
        if (newCapturedTotal >= Amount.Amount)
            Status = PaymentStatus.Captured;
        else
            Status = PaymentStatus.PartiallyCaptured;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentCapturedDomainEvent(Id, MerchantId, transaction.Id, captureAmount));
    }

    public void Void(string? idempotencyKey = null)
    {
        if (Status != PaymentStatus.Authorized)
            throw new InvalidOperationException($"Cannot void payment in '{Status}' status.");

        Status = PaymentStatus.Voided;
        UpdatedAt = DateTime.UtcNow;

        var transaction = new Transaction(TransactionType.Void, Amount, idempotencyKey: idempotencyKey);
        _transactions.Add(transaction);

        AddDomainEvent(new PaymentVoidedDomainEvent(Id));
    }

    public void Refund(Money? amount = null, string? idempotencyKey = null)
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

        var transaction = new Transaction(TransactionType.Refund, refundAmount, idempotencyKey: idempotencyKey);
        _transactions.Add(transaction);

        var newRefundedTotal = totalRefunded + refundAmount.Amount;
        if (newRefundedTotal >= totalCaptured)
            Status = PaymentStatus.FullyRefunded;
        else
            Status = PaymentStatus.PartiallyRefunded;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new PaymentRefundedDomainEvent(Id, MerchantId, transaction.Id, refundAmount));
    }

    public void Fail()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot fail payment in '{Status}' status.");

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Expires a payment intent that was never finalized. Allowed only from the
    /// in-flight states (Pending / RequiresAction / Processing) — a merchant may
    /// not authorize or capture it afterward. Idempotent: expiring an already
    /// expired intent is a no-op so the worker can run safely alongside replay.
    /// </summary>
    /// <remarks>
    /// Reversal semantics are intentionally NOT a second terminal state: an
    /// authorized intent is reversed via <c>Void()</c>; a captured intent is
    /// reversed via <c>Refund()</c>. <c>Expired</c> only terminates intents that
    /// never left the starting states.
    /// </remarks>
    public void Expire()
    {
        if (Status == PaymentStatus.Expired)
            return;

        if (Status != PaymentStatus.Pending
            && Status != PaymentStatus.RequiresAction
            && Status != PaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot expire payment in '{Status}' status.");

        Status = PaymentStatus.Expired;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentExpiredDomainEvent(Id, MerchantId, Amount));
    }

    private long GetTotalCaptured() =>
        _transactions.Where(t => t.Type == TransactionType.Capture).Sum(t => t.Amount.Amount);

    private long GetTotalRefunded() =>
        _transactions.Where(t => t.Type == TransactionType.Refund).Sum(t => t.Amount.Amount);
}