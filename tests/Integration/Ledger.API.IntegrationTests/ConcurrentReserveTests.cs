using Ledger.Application.Features.Commands.ReserveFunds;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves the first-payment race is safe: two parallel reserves for a merchant
/// with no account yet must yield exactly one account with both postings,
/// instead of a unique-violation 500 losing one reservation.
/// </summary>
public class ConcurrentReserveTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public ConcurrentReserveTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ParallelReserves_ForNewMerchant_ProduceOneAccountAndBothPostings()
    {
        var merchantId = Guid.NewGuid();

        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        var handler1 = scope1.ServiceProvider.GetRequiredService<ReserveFundsHandler>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<ReserveFundsHandler>();

        var results = await Task.WhenAll(
            handler1.Handle(new ReserveFundsCommand(merchantId, 100L, "USD", "race-a")),
            handler2.Handle(new ReserveFundsCommand(merchantId, 200L, "USD", "race-b")));

        Assert.All(results, r => Assert.True(r.IsSuccess));

        using var dbScope = _factory.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var accounts = await db.LedgerAccounts
            .Include(a => a.Journal)
            .Where(a => a.MerchantId == merchantId)
            .ToListAsync();
        var account = Assert.Single(accounts);
        Assert.Equal(300L, account.PendingBalance);
        Assert.Equal(300L, account.ReservedBalance);
        Assert.Equal(2, account.Journal.Count(j => j.Description == "Funds reserved"));
    }
}
