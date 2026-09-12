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

    [Fact]
    public async Task DailyPayout_RefundHeavyDay_ClampsGrossAtZero()
    {
        // Capture on day 1, partial refund on day 2: the refund day has no
        // captures, so its raw gross is negative. The payout must floor at
        // zero instead of aborting downstream batch creation in Money(negative).
        var day1 = new DateTime(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
        var day2 = day1.AddDays(1);
        var merchant = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account = new LedgerAccount(Guid.NewGuid(), merchant, "USD");
            account.ReserveFunds(new Money(3000, "USD"), new CorrelationId("reserve-c"));
            account.CaptureFunds(new Money(3000, "USD"), new CorrelationId("capture-c"));
            account.ChargeFees(new Money(150, "USD"), new CorrelationId("fees-c"));
            account.RefundFunds(new Money(2000, "USD"), new CorrelationId("refund-c"));
            db.LedgerAccounts.Add(account);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"JournalEntries\" SET \"Timestamp\" = {0} WHERE \"LedgerAccountId\" = {1} AND \"Description\" <> 'Funds refunded'",
                day1, account.Id);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"JournalEntries\" SET \"Timestamp\" = {0} WHERE \"LedgerAccountId\" = {1} AND \"Description\" = 'Funds refunded'",
                day2, account.Id);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var query = scope.ServiceProvider.GetRequiredService<IDailyPayoutQuery>();

            var day1Payouts = await query.GetAsync(day1);
            var payoutDay1 = Assert.Single(day1Payouts, p => p.MerchantId == merchant);
            Assert.Equal(3000, payoutDay1.GrossVolume);
            Assert.Equal(150, payoutDay1.Fees);

            var day2Payouts = await query.GetAsync(day2);
            var payoutDay2 = Assert.Single(day2Payouts, p => p.MerchantId == merchant);
            Assert.Equal(0, payoutDay2.GrossVolume);
            Assert.Equal(0, payoutDay2.Fees);
        }
    }
}
