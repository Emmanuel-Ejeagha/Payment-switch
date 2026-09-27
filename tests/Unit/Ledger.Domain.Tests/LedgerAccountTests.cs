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
    public void ReserveFunds_ShouldIncreasePendingAndReserved()
    {
        var account = CreateAccountWithAvailable(1000L);

        var amount = new Money(200L, "USD");
        var correlationId = new CorrelationId("PaymentAuthorized:123");
        account.ReserveFunds(amount, correlationId);

        Assert.Equal(1000L, account.AvailableBalance);
        Assert.Equal(200L, account.PendingBalance);
        Assert.Equal(200L, account.ReservedBalance);
        var entry = Assert.Single(account.Journal);
        Assert.Equal(EntryType.Credit, entry.Type);
        Assert.Equal(GlAccountCode.Cash, entry.DebitAccount);
        Assert.Equal(GlAccountCode.Reserve, entry.CreditAccount);
        Assert.Equal(200L, entry.Amount.Amount);
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
        Assert.Equal(150L, account.ReservedBalance);
        var entry = Assert.Single(account.Journal);
        Assert.Equal(EntryType.Credit, entry.Type);
        Assert.Equal(GlAccountCode.Reserve, entry.DebitAccount);
        Assert.Equal(GlAccountCode.MerchantLiability, entry.CreditAccount);
        Assert.Equal(150L, entry.Amount.Amount);
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
        var entry = Assert.Single(account.Journal);
        Assert.Equal(EntryType.Debit, entry.Type);
        Assert.Equal(GlAccountCode.MerchantLiability, entry.DebitAccount);
        Assert.Equal(GlAccountCode.Cash, entry.CreditAccount);
        Assert.Equal(200L, entry.Amount.Amount);
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

    [Fact]
    public void ChargeFees_ShouldReduceAvailableAndBookFeesIncome()
    {
        var account = CreateAccountWithAvailable(1000L);
        var fees = new Money(15L, "USD");
        var correlationId = new CorrelationId("PaymentCaptured:456");
        account.ChargeFees(fees, correlationId);

        Assert.Equal(985L, account.AvailableBalance);
        var entry = Assert.Single(account.Journal);
        Assert.Equal(EntryType.Debit, entry.Type);
        Assert.Equal(GlAccountCode.MerchantLiability, entry.DebitAccount);
        Assert.Equal(GlAccountCode.FeesIncome, entry.CreditAccount);
        Assert.Equal(15L, entry.Amount.Amount);
        Assert.Contains(account.DomainEvents, e => e is FeesChargedEvent);
    }

    [Fact]
    public void ChargeFees_InsufficientAvailable_ShouldThrow()
    {
        var account = CreateAccountWithAvailable(5L);
        var fees = new Money(15L, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<InvalidOperationException>(() => account.ChargeFees(fees, correlationId));
    }

    [Fact]
    public void ChargeFees_ZeroAmount_ShouldBeNoOp()
    {
        var account = CreateAccountWithAvailable(1000L);
        var fees = new Money(0L, "USD");
        var correlationId = new CorrelationId("test");

        account.ChargeFees(fees, correlationId);

        Assert.Equal(1000L, account.AvailableBalance);
        Assert.Empty(account.Journal);
        Assert.DoesNotContain(account.DomainEvents, e => e is FeesChargedEvent);
    }

    [Fact]
    public void JournalEntry_DebitAndCreditAccountsMustDiffer()
    {
        var amount = new Money(100L, "USD");
        var correlationId = new CorrelationId("test");

        Assert.Throws<ArgumentException>(() =>
            new JournalEntry(EntryType.Credit, GlAccountCode.Cash, GlAccountCode.Cash, amount, "test", correlationId));
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
        account.ReservedBalance = amount;
        account.ClearDomainEvents();
        return account;
    }

    [Fact]
    public void ReleaseFunds_ShouldDecreasePendingAndReserved()
    {
        var account = CreateAccountWithPending(500L);

        var amount = new Money(500L, "USD");
        var correlationId = new CorrelationId("PaymentVoid:123");
        account.ReleaseFunds(amount, correlationId);

        Assert.Equal(0L, account.PendingBalance);
        Assert.Equal(0L, account.ReservedBalance);
        Assert.Equal(0L, account.AvailableBalance);
        var entry = Assert.Single(account.Journal);
        Assert.Equal(EntryType.Debit, entry.Type);
        Assert.Equal(GlAccountCode.Reserve, entry.DebitAccount);
        Assert.Equal(GlAccountCode.Cash, entry.CreditAccount);
        Assert.Equal(500L, entry.Amount.Amount);
        Assert.Contains(account.DomainEvents, e => e is FundsReleasedEvent);
    }

    [Fact]
    public void ReleaseFunds_PartialRelease_ShouldLeaveRemainderReserved()
    {
        var account = CreateAccountWithPending(500L);

        account.ReleaseFunds(new Money(200L, "USD"), new CorrelationId("PaymentVoid:123"));

        Assert.Equal(300L, account.PendingBalance);
        Assert.Equal(300L, account.ReservedBalance);
    }

    [Fact]
    public void ReleaseFunds_CurrencyMismatch_ShouldThrow()
    {
        var account = CreateAccountWithPending(500L);

        Assert.Throws<InvalidOperationException>(
            () => account.ReleaseFunds(new Money(100L, "EUR"), new CorrelationId("test")));
    }

    [Fact]
    public void ReleaseFunds_ExceedingReserved_ShouldThrow()
    {
        var account = CreateAccountWithPending(100L);

        Assert.Throws<InvalidOperationException>(
            () => account.ReleaseFunds(new Money(500L, "USD"), new CorrelationId("test")));
    }
}
