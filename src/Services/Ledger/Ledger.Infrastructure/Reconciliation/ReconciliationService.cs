using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Ledger.Domain.Enums;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Reconciliation;

/// <summary>
/// Recomputes each account's balances purely from its journal entries (the
/// append-only audit trail) and compares them against the denormalized stored
/// balances. Capture/Refund/Fee postings each emit exactly one journal row, so
/// expected = Σ credits − Σ debits per balance.
/// </summary>
public class ReconciliationService : IReconciliationService
{
    private readonly AppDbContext _db;

    public ReconciliationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ReconciliationLineItem>> ComputeAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await _db.LedgerAccounts
            .Include(a => a.Journal)
            .OrderBy(a => a.MerchantId)
            .ThenBy(a => a.Currency)
            .ToListAsync(cancellationToken);

        var items = new List<ReconciliationLineItem>(accounts.Count);
        foreach (var account in accounts)
        {
            var expected = ComputeExpected(account);
            items.Add(new ReconciliationLineItem(
                account.MerchantId,
                account.Currency,
                expected.Available,
                account.AvailableBalance,
                expected.Pending,
                account.PendingBalance,
                expected.Reserved,
                account.ReservedBalance));
        }

        return items;
    }

    private static ExpectedBalances ComputeExpected(LedgerAccount account)
    {
        long reserved = 0, pending = 0, available = 0;
        foreach (var entry in account.Journal)
        {
            // Recompute strictly from GL legs, not description strings: a swapped
            // debit/credit pair with the same description must not tie out.
            if (entry.DebitAccount == GlAccountCode.Cash && entry.CreditAccount == GlAccountCode.Reserve)
            {
                reserved += entry.Amount.Amount;
                pending += entry.Amount.Amount;
            }
            else if (entry.DebitAccount == GlAccountCode.Reserve && entry.CreditAccount == GlAccountCode.MerchantLiability)
            {
                reserved -= entry.Amount.Amount;
                pending -= entry.Amount.Amount;
                available += entry.Amount.Amount;
            }
            else if (entry.DebitAccount == GlAccountCode.Reserve && entry.CreditAccount == GlAccountCode.Cash)
            {
                reserved -= entry.Amount.Amount;
                pending -= entry.Amount.Amount;
            }
            else if (entry.DebitAccount == GlAccountCode.MerchantLiability && entry.CreditAccount == GlAccountCode.Cash)
            {
                available -= entry.Amount.Amount;
            }
            else if (entry.DebitAccount == GlAccountCode.MerchantLiability && entry.CreditAccount == GlAccountCode.FeesIncome)
            {
                available -= entry.Amount.Amount;
            }
        }

        return new ExpectedBalances(available, pending, reserved);
    }

    private readonly record struct ExpectedBalances(long Available, long Pending, long Reserved);
}