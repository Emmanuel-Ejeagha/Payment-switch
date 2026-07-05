using Payment.Domain.DomainEvents;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Tests;

public class PaymentIntentTests
{
    private readonly Guid _merchantId = Guid.NewGuid();
    private readonly Money _amount = new(100m, "USD");
    private readonly IdempotencyKey _idempotencyKey = new("unique-key-123");

    [Fact]
    public void Constructor_ShouldCreatePendingIntentAndRaiseEvent()
    {
        var intent = new PaymentIntent(Guid.NewGuid(), _merchantId, _amount, _idempotencyKey, PaymentMethod.Card);

        Assert.Equal(PaymentStatus.Pending, intent.Status);
        Assert.Equal(_amount, intent.Amount);
        Assert.Single(intent.DomainEvents, e => e is PaymentIntentCreatedDomainEvent);
    }

    [Fact]
    public void Constructor_WithNullAmount_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new PaymentIntent(Guid.NewGuid(), _merchantId, null!, _idempotencyKey, PaymentMethod.Card));
    }

    [Fact]
    public void Authorize_FromPending_ShouldSetAuthorizedAndRaiseEvent()
    {
        var intent = CreatePendingIntent();
        var authCode = new AuthorizationCode("AUTH123");
        var gatewayRef = new GatewayReference("GTW-1");

        intent.Authorize(authCode, gatewayRef);

        Assert.Equal(PaymentStatus.Authorized, intent.Status);
        Assert.Equal(authCode, intent.AuthorizationCode);
        Assert.Equal(gatewayRef, intent.GatewayReference);
        Assert.Single(intent.Transactions, t => t.Type == TransactionType.Authorization);
        Assert.Contains(intent.DomainEvents, e => e is PaymentAuthorizedDomainEvent);
    }

    [Fact]
    public void Authorize_FromNonPending_Throws()
    {
        var intent = CreatePendingIntent();
        intent.Authorize(new AuthorizationCode("A"), new GatewayReference("G"));

        Assert.Throws<InvalidOperationException>(() => intent.Authorize(new AuthorizationCode("B"), new GatewayReference("G2")));
    }

    [Fact]
    public void Capture_Full_FromAuthorized_ShouldCaptureAndSetStatus()
    {
        var intent = CreateAuthorizedIntent();
        var captureAmount = new Money(100m, "USD");

        intent.Capture(captureAmount);

        Assert.Equal(PaymentStatus.Captured, intent.Status);
        Assert.Single(intent.Transactions, t => t.Type == TransactionType.Capture);
        Assert.Contains(intent.DomainEvents, e => e is PaymentCapturedDomainEvent);
    }

    [Fact]
    public void Capture_Partial_ShouldSetPartiallyCaptured()
    {
        var intent = CreateAuthorizedIntent();
        var captureAmount = new Money(40m, "USD");

        intent.Capture(captureAmount);

        Assert.Equal(PaymentStatus.PartiallyCaptured, intent.Status);
    }

    [Fact]
    public void Capture_ExceedsAuthorized_Throws()
    {
        var intent = CreateAuthorizedIntent();
        var captureAmount = new Money(200m, "USD");

        Assert.Throws<InvalidOperationException>(() => intent.Capture(captureAmount));
    }

    [Fact]
    public void Capture_FromPending_Throws()
    {
        var intent = CreatePendingIntent();
        Assert.Throws<InvalidOperationException>(() => intent.Capture(new Money(10m, "USD")));
    }

    [Fact]
    public void Void_FromAuthorized_ShouldVoid()
    {
        var intent = CreateAuthorizedIntent();

        intent.Void();

        Assert.Equal(PaymentStatus.Voided, intent.Status);
        Assert.Single(intent.Transactions, t => t.Type == TransactionType.Void);
        Assert.Contains(intent.DomainEvents, e => e is PaymentVoidedDomainEvent);
    }

    [Fact]
    public void Void_FromNonAuthorized_Throws()
    {
        var intent = CreatePendingIntent();
        Assert.Throws<InvalidOperationException>(() => intent.Void());
    }

    [Fact]
    public void Refund_Full_FromCaptured_ShouldRefundAndSetStatus()
    {
        var intent = CreateCapturedIntent();
        var refundAmount = new Money(100m, "USD");

        intent.Refund(refundAmount);

        Assert.Equal(PaymentStatus.FullyRefunded, intent.Status);
        Assert.Single(intent.Transactions, t => t.Type == TransactionType.Refund);
        Assert.Contains(intent.DomainEvents, e => e is PaymentRefundedDomainEvent);
    }

    [Fact]
    public void Refund_Partial_ShouldSetPartiallyRefunded()
    {
        var intent = CreateCapturedIntent();
        var refundAmount = new Money(30m, "USD");

        intent.Refund(refundAmount);

        Assert.Equal(PaymentStatus.PartiallyRefunded, intent.Status);
    }

    [Fact]
    public void Refund_ExceedsCaptured_Throws()
    {
        var intent = CreateCapturedIntent();
        var refundAmount = new Money(200m, "USD");

        Assert.Throws<InvalidOperationException>(() => intent.Refund(refundAmount));
    }

    [Fact]
    public void Refund_FromUnauthorizedStatus_Throws()
    {
        var intent = CreatePendingIntent();
        Assert.Throws<InvalidOperationException>(() => intent.Refund(new Money(10m, "USD")));
    }

    [Fact]
    public void Fail_FromPending_ShouldFail()
    {
        var intent = CreatePendingIntent();
        intent.Fail();
        Assert.Equal(PaymentStatus.Failed, intent.Status);
    }

    [Fact]
    public void Fail_FromAuthorized_Throws()
    {
        var intent = CreateAuthorizedIntent();
        Assert.Throws<InvalidOperationException>(() => intent.Fail());
    }

    [Fact]
    public void Money_InvalidAmount_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(0m, "USD"));
        Assert.Throws<ArgumentException>(() => new Money(-5m, "USD"));
    }

    [Fact]
    public void Money_InvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(10m, "US"));
        Assert.Throws<ArgumentException>(() => new Money(10m, ""));
    }

    [Fact]
    public void IdempotencyKey_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new IdempotencyKey(""));
    }

    // Helper methods
    private PaymentIntent CreatePendingIntent() =>
        new(Guid.NewGuid(), _merchantId, _amount, _idempotencyKey, PaymentMethod.Card);

    private PaymentIntent CreateAuthorizedIntent()
    {
        var intent = CreatePendingIntent();
        intent.Authorize(new AuthorizationCode("AUTH"), new GatewayReference("GW"));
        intent.ClearDomainEvents();
        return intent;
    }

    private PaymentIntent CreateCapturedIntent()
    {
        var intent = CreateAuthorizedIntent();
        intent.Capture(new Money(100m, "USD"));
        intent.ClearDomainEvents();
        return intent;
    }
}