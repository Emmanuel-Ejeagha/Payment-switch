using Ledger.Domain.Enums;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Queries;

public record DailyPayout(Guid MerchantId, string Currency, long GrossVolume, long Fees);

public interface IDailyPayoutQuery
{
    Task<List<DailyPayout>> GetAsync(DateTime utcDate, CancellationToken cancellationToken = default);
}

public class DailyPayoutQuery : IDailyPayoutQuery
{
    private readonly AppDbContext _db;

    public DailyPayoutQuery(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DailyPayout>> GetAsync(DateTime utcDate, CancellationToken cancellationToken = default)
    {
        var start = DateTime.SpecifyKind(utcDate.Date, DateTimeKind.Utc);
        var end = start.AddDays(1);

        var rows = await (
            from a in _db.LedgerAccounts
            from e in a.Journal
            where e.Timestamp >= start && e.Timestamp < end
            group e by new { a.MerchantId, a.Currency } into g
            select new
            {
                g.Key.MerchantId,
                g.Key.Currency,
                Captures = g.Sum(e => e.CreditAccount == GlAccountCode.MerchantLiability ? e.Amount.Amount : 0),
                Refunds = g.Sum(e => e.DebitAccount == GlAccountCode.MerchantLiability && e.Description == "Funds refunded" ? e.Amount.Amount : 0),
                Fees = g.Sum(e => e.CreditAccount == GlAccountCode.FeesIncome ? e.Amount.Amount : 0)
            }).ToListAsync(cancellationToken);

        return rows
            .Where(r => r.Captures - r.Refunds != 0 || r.Fees != 0)
            .Select(r => new DailyPayout(r.MerchantId, r.Currency, r.Captures - r.Refunds, r.Fees))
            .ToList();
    }
}
