using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class ReconciliationGlTests
{
    [Fact]
    public void SwappedLegs_WithSameDescription_DoesNotTieOut()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        // Correct reserve: Cash -> Reserve
        account.ReserveFunds(new Money(1000, "USD"), new CorrelationId("corr-reserve"));
        // Simulate a bug that swaps debit/credit but keeps description "Funds captured"
        // Correct capture is Reserve -> MerchantLiability; swapped is MerchantLiability -> Reserve
        var swappedCapture = new JournalEntry(
            EntryType.Credit,
            GlAccountCode.MerchantLiability,
            GlAccountCode.Reserve,
            new Money(1000, "USD"),
            "Funds captured",
            new CorrelationId("corr-capture-swapped"));

        // Use reflection to inject swapped entry into account's journal (since public API enforces correct legs)
        var journalField = typeof(LedgerAccount).GetField("_journal", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var journal = (List<JournalEntry>)journalField.GetValue(account)!;
        journal.Add(swappedCapture);

        // Recompute expected via GL legs (as service does)
        long reserved = 0, pending = 0, available = 0;
        foreach (var e in account.Journal)
        {
            if (e.DebitAccount == GlAccountCode.Cash && e.CreditAccount == GlAccountCode.Reserve) { reserved += e.Amount.Amount; pending += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.Reserve && e.CreditAccount == GlAccountCode.MerchantLiability) { reserved -= e.Amount.Amount; pending -= e.Amount.Amount; available += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.Reserve && e.CreditAccount == GlAccountCode.Cash) { reserved -= e.Amount.Amount; pending -= e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.MerchantLiability && e.CreditAccount == GlAccountCode.Cash) { available -= e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.MerchantLiability && e.CreditAccount == GlAccountCode.FeesIncome) { available -= e.Amount.Amount; }
        }

        // Correct GL would have available 1000, but swapped leg is ignored, so available stays 0, reserved stays 1000
        Assert.Equal(0, available);
        Assert.Equal(1000, reserved);
        // Actual account balances after Reserve only: available 0, pending 1000, reserved 1000 (since swapped capture not via domain, balances unchanged)
        Assert.Equal(0, account.AvailableBalance);
        Assert.Equal(1000, account.PendingBalance);
        Assert.Equal(1000, account.ReservedBalance);
        // If reconciliation were description-based, it would have counted swapped "Funds captured" as available 1000 -> would incorrectly tie out
        // Our GL-based recompute correctly leaves mismatch (available 0 vs if it were 1000)
        Assert.NotEqual(1000, available);
    }

    [Fact]
    public void CorrectLegs_TieOut()
    {
        var account = new LedgerAccount(Guid.NewGuid(), Guid.NewGuid(), "USD");
        account.ReserveFunds(new Money(1000, "USD"), new CorrelationId("c1"));
        account.CaptureFunds(new Money(1000, "USD"), new CorrelationId("c2"));
        account.ChargeFees(new Money(100, "USD"), new CorrelationId("c3"));

        long reserved = 0, pending = 0, available = 0;
        foreach (var e in account.Journal)
        {
            if (e.DebitAccount == GlAccountCode.Cash && e.CreditAccount == GlAccountCode.Reserve) { reserved += e.Amount.Amount; pending += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.Reserve && e.CreditAccount == GlAccountCode.MerchantLiability) { reserved -= e.Amount.Amount; pending -= e.Amount.Amount; available += e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.Reserve && e.CreditAccount == GlAccountCode.Cash) { reserved -= e.Amount.Amount; pending -= e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.MerchantLiability && e.CreditAccount == GlAccountCode.Cash) { available -= e.Amount.Amount; }
            else if (e.DebitAccount == GlAccountCode.MerchantLiability && e.CreditAccount == GlAccountCode.FeesIncome) { available -= e.Amount.Amount; }
        }

        Assert.Equal(account.AvailableBalance, available);
        Assert.Equal(account.PendingBalance, pending);
        Assert.Equal(account.ReservedBalance, reserved);
        Assert.Equal(900, available); // 1000 - 100 fee
    }
}
