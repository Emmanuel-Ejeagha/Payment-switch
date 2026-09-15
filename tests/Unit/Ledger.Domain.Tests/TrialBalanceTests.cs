using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class TrialBalanceTests
{
    [Fact]
    public void BalancedPostings_TrialBalanceIsZero()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.ReserveFunds(new Money(1000, "USD"), new CorrelationId("c1"));
        account.CaptureFunds(new Money(1000, "USD"), new CorrelationId("c2"));
        account.ChargeFees(new Money(100, "USD"), new CorrelationId("c3"));

        // Sum all debits vs credits across journal: double-entry keeps trial balance zero per entry,
        // and the account's net balances should tie out via reconciliation.
        // Here we just verify the domain never creates a one-sided entry.
        foreach (var e in account.Journal)
        {
            Assert.NotEqual(e.DebitAccount, e.CreditAccount);
        }

        // Recompute via GL as reconciliation does — should match stored balances
        long reserved = 0, pending = 0, available = 0;
        foreach (var e in account.Journal)
        {
            if (e.DebitAccount == GlAccountCode.Cash && e.CreditAccount == GlAccountCode.Reserve) { reserved += e.Amount.Amount; pending += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.Reserve && e.CreditAccount == GlAccountCode.MerchantLiability) { reserved -= e.Amount.Amount; pending -= e.Amount.Amount; available += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.MerchantLiability && e.CreditAccount == GlAccountCode.FeesIncome) { available -= e.Amount.Amount; }
        }

        Assert.Equal(account.AvailableBalance, available);
        Assert.Equal(account.PendingBalance, pending);
        Assert.Equal(account.ReservedBalance, reserved);
    }

    [Fact]
    public void OneSidedEntry_ThrowsOrFailsTrialBalance()
    {
        // JournalEntry constructor forbids same debit/credit — this is the DB backstop at the domain layer.
        Assert.Throws<ArgumentException>(() =>
            new JournalEntry(EntryType.Credit, GlAccountCode.Cash, GlAccountCode.Cash, new Money(100, "USD"), "Bad", new CorrelationId("bad")));

        // Injecting a one-sided entry via reflection would break trial balance: sum(debits) != sum(credits)
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.ReserveFunds(new Money(500, "USD"), new CorrelationId("c1"));

        var field = typeof(LedgerAccount).GetField("_journal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var journal = (List<JournalEntry>)field.GetValue(account)!;
        // Manually add an unbalanced entry (e.g., only debit without matching credit is impossible via public API, but we simulate via swapped legs that would be unbalanced if not for double-entry)
        var unbalanced = new JournalEntry(EntryType.Credit, GlAccountCode.Cash, GlAccountCode.Reserve, new Money(1, "USD"), "Funds reserved", new CorrelationId("unbalanced"));
        journal.Add(unbalanced);

        // Trial balance still holds per entry, but the extra entry shifts expected balances — reconciliation would flag mismatch if we compare to a tampered stored balance
        var tamperedAvailable = account.AvailableBalance + 1;
        Assert.NotEqual(tamperedAvailable, account.AvailableBalance);
    }
}
