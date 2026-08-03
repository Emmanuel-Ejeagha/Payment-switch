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

        Assert.Equal(0L, account.AvailableBalance);
        Assert.Equal(0L, account.PendingBalance);
        Assert.Equal(0L, account.ReservedBalance);
        Assert.Equal("USD", account.Currency);
    }

    [Fact]
    public void ReserveFunds_ShouldIncreasePending()
    {
        var account = CreateAccountWithAvailable(1000L);

        var amount = new Money(200L, "USD");
        var correlationId = new CorrelationId("PaymentAuthorized:123");
        account.ReserveFunds(amount, correlationId);

        Assert.Equal(1000L, account.AvailableBalance);
        Assert.Equal(200L, account.PendingBalance);
        Assert.Single(account.Journal, j => j.Type == EntryType.Debit && j.Amount.Amount == 200L);
        Assert.Contains(account.DomainEvents, e => e is FundsReservedEvent);
    }

    [Fact]
    public void ReserveFunds_CurrencyMismatch_ShouldThrow()
    {
        var account = CreateAccountWithAvailable(50L);
        var amount = new Money(100L, "EUR");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.ReserveFunds(amount, correlationId));
    }

    [Fact]
    public void CaptureFunds_ShouldMovePendingToAvailable()
    {
        var account = CreateAccountWithPending(300L);
        var amount = new Money(150L, "USD");
        var correlationId = new CorrelationId("PaymentCaptured:456");
        account.CaptureFunds(amount, correlationId);

        Assert.Equal(150L, account.AvailableBalance);
        Assert.Equal(150L, account.PendingBalance);
        Assert.Contains(account.Journal, j => j.Type == EntryType.Credit && j.Amount.Amount == 150L);
        Assert.Contains(account.DomainEvents, e => e is FundsCapturedEvent);
    }

    [Fact]
    public void CaptureFunds_InsufficientPending_ShouldThrow()
    {
        var account = CreateAccountWithPending(50L);
        var amount = new Money(100L, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.CaptureFunds(amount, correlationId));
    }

    [Fact]
    public void RefundFunds_ShouldReduceAvailable()
    {
        var account = CreateAccountWithAvailable(1000L);
        var amount = new Money(200L, "USD");
        var correlationId = new CorrelationId("PaymentRefunded:789");
        account.RefundFunds(amount, correlationId);

        Assert.Equal(800L, account.AvailableBalance);
        Assert.Contains(account.Journal, j => j.Type == EntryType.Debit && j.Amount.Amount == 200L);
        Assert.Contains(account.DomainEvents, e => e is FundsRefundedEvent);
    }

    [Fact]
    public void RefundFunds_InsufficientAvailable_ShouldThrow()
    {
        var account = CreateAccountWithAvailable(10L);
        var amount = new Money(50L, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.RefundFunds(amount, correlationId));
    }

    private LedgerAccount CreateAccountWithAvailable(long amount)
    {
        var account = new LedgerAccount(Guid.NewGuid(), _merchantId, "USD");
        account.AvailableBalance = amount;
        account.ClearDomainEvents();
        return account;
    }

    private LedgerAccount CreateAccountWithPending(long amount)
    {
        var account = new LedgerAccount(Guid.NewGuid(), _merchantId, "USD");
        account.PendingBalance = amount;
        account.ClearDomainEvents();
        return account;
    }
}