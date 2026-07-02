using Ledger.Domain.DomainEvents;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class LedgerAccountTests
{
    private readonly Guid _merchantId = Guid.NewGuid();

    [Fact]
    public void Constructor_ShouldInitializeWithZeroBalances()
    {
        var account = new LedgerAccount(Guid.NewGuid(), _merchantId, "USD");

        Assert.Equal(0m, account.AvailableBalance);
        Assert.Equal(0m, account.PendingBalance);
        Assert.Equal(0m, account.ReservedBalance);
        Assert.Equal("USD", account.Currency);
    }

    [Fact]
    public void ReserveFunds_ShouldMoveAvailableToPending()
    {
        var account = CreateAccountWithAvailable(1000m);

        var amount = new Money(200m, "USD");
        var correlationId = new CorrelationId("PaymentAuthorized:123");
        account.ReserveFunds(amount, correlationId);

        Assert.Equal(800m, account.AvailableBalance);
        Assert.Equal(200m, account.PendingBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Debit && j.Amount.Amount == 200m);
        Assert.Contains(account.DomainEvents, e => e is FundsReservedEvent);
    }

    [Fact]
    public void ReserveFunds_InsufficientAvailable_ShouldThrow()
    {
        var account = CreateAccountWithAvailable(50m);
        var amount = new Money(100m, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.ReserveFunds(amount, correlationId));
    }

    [Fact]
    public void CaptureFunds_ShouldMovePendingToAvailable()
    {
        var account = CreateAccountWithPending(300m);
        var amount = new Money(150m, "USD");
        var correlationId = new CorrelationId("PaymentCaptured:456");
        account.CaptureFunds(amount, correlationId);

        Assert.Equal(150m, account.AvailableBalance);
        Assert.Equal(150m, account.PendingBalance);
        Assert.Contains(account.Journal, j => j.Type == EntryType.Credit && j.Amount.Amount == 150m);
        Assert.Contains(account.DomainEvents, e => e is FundsCapturedEvent);
    }

    [Fact]
    public void CaptureFunds_InsufficientPending_ShouldThrow()
    {
        var account = CreateAccountWithPending(50m);
        var amount = new Money(100m, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.CaptureFunds(amount, correlationId));
    }

    [Fact]
    public void RefundFunds_ShouldReduceAvailable()
    {
        var account = CreateAccountWithAvailable(1000m);
        var amount = new Money(200m, "USD");
        var correlationId = new CorrelationId("PaymentRefunded:789");
        account.RefundFunds(amount, correlationId);

        Assert.Equal(800m, account.AvailableBalance);
        Assert.Contains(account.Journal, j => j.Type == EntryType.Debit && j.Amount.Amount == 200m);
        Assert.Contains(account.DomainEvents, e => e is FundsRefundedEvent);
    }

    [Fact]
    public void RefundFunds_InsufficientAvailable_ShouldThrow()
    {
        var account = CreateAccountWithAvailable(10m);
        var amount = new Money(50m, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.RefundFunds(amount, correlationId));
    }

    private LedgerAccount CreateAccountWithAvailable(decimal amount)
    {
        var account = new LedgerAccount(Guid.NewGuid(), _merchantId, "USD");
        account.AvailableBalance = amount;
        account.ClearDomainEvents();
        return account;
    }

    private LedgerAccount CreateAccountWithPending(decimal amount)
    {
        var account = new LedgerAccount(Guid.NewGuid(), _merchantId, "USD");
        account.PendingBalance = amount;
        account.ClearDomainEvents();
        return account;
    }
}