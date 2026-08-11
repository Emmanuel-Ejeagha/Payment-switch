using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Ledger.Infrastructure.Persistence;
using Ledger.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves GetDailyPayoutData aggregation: gross = Σ captures − Σ refunds and fees
/// come from FeesIncome, scoped strictly to the requested UTC day.
/// </summary>
public class DailyPayoutQueryTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public DailyPayoutQueryTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DailyPayout_AggregatesCapturesRefundsAndFees_ForThatDateOnly()
    {
        var day = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var merchantA = Guid.NewGuid();
        var merchantB = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accountA = new LedgerAccount(Guid.NewGuid(), merchantA, "USD");
            accountA.ReserveFunds(new Money(5000, "USD"), new CorrelationId("reserve-a"));
            accountA.CaptureFunds(new Money(5000, "USD"), new CorrelationId("capture-a"));

            var accountB = new LedgerAccount(Guid.NewGuid(), merchantB, "USD");
            accountB.ReserveFunds(new Money(3000, "USD"), new CorrelationId("reserve-b"));
            accountB.CaptureFunds(new Money(3000, "USD"), new CorrelationId("capture-b"));
            accountB.RefundFunds(new Money(1000, "USD"), new CorrelationId("refund-b"));
            accountB.ChargeFees(new Money(150, "USD"), new CorrelationId("fees-b"));

            db.LedgerAccounts.AddRange(accountA, accountB);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"JournalEntries\" SET \"Timestamp\" = {0}", day);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<IDailyPayoutQuery>();

            var payouts = await query.GetAsync(day);
            Assert.Equal(2, payouts.Count);

            var payoutA = payouts.Single(p => p.MerchantId == merchantA);
            Assert.Equal(5000, payoutA.GrossVolume);
            Assert.Equal(0, payoutA.Fees);
            Assert.Equal("USD", payoutA.Currency);

            var payoutB = payouts.Single(p => p.MerchantId == merchantB);
            Assert.Equal(2000, payoutB.GrossVolume);
            Assert.Equal(150, payoutB.Fees);

            var otherDay = await query.GetAsync(day.AddDays(1));
            Assert.Empty(otherDay);
        }
    }
}
