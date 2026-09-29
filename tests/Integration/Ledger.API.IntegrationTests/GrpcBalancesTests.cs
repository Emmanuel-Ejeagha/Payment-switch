using Ledger.API.Services;
using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Ledger.Infrastructure.Persistence;
using Ledger.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentSwitch.Protos.Ledger;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves GetBalances no longer returns an arbitrary single account: every
/// currency account is returned, filterable by currency, and unknown merchants
/// get an empty list instead of fabricated USD zeros.
/// </summary>
public class GrpcBalancesTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public GrpcBalancesTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetBalances_ReturnsEveryCurrencyAccount()
    {
        var merchantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usd = new LedgerAccount(Guid.NewGuid(), merchantId, "USD");
        usd.ReserveFunds(new Money(5000, "USD"), new CorrelationId("bal-usd"));
        var eur = new LedgerAccount(Guid.NewGuid(), merchantId, "EUR");
        eur.ReserveFunds(new Money(3000, "EUR"), new CorrelationId("bal-eur"));
        db.LedgerAccounts.AddRange(usd, eur);
        await db.SaveChangesAsync();

        var service = new LedgerGrpcService(db, scope.ServiceProvider.GetRequiredService<IDailyPayoutQuery>());
        var response = await service.GetBalances(new GetBalancesRequest { MerchantId = merchantId.ToString() }, null!);

        Assert.Equal(2, response.Balances.Count);
        var usdBalance = Assert.Single(response.Balances, b => b.Currency == "USD");
        Assert.Equal(0, usdBalance.Available);
        Assert.Equal(5000, usdBalance.Pending);
        Assert.Equal(5000, usdBalance.Reserved);
        var eurBalance = Assert.Single(response.Balances, b => b.Currency == "EUR");
        Assert.Equal(3000, eurBalance.Pending);
    }

    [Fact]
    public async Task GetBalances_CurrencyFilter_ReturnsSingleAccount()
    {
        var merchantId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var usd = new LedgerAccount(Guid.NewGuid(), merchantId, "USD");
        usd.ReserveFunds(new Money(5000, "USD"), new CorrelationId("balf-usd"));
        var eur = new LedgerAccount(Guid.NewGuid(), merchantId, "EUR");
        eur.ReserveFunds(new Money(3000, "EUR"), new CorrelationId("balf-eur"));
        db.LedgerAccounts.AddRange(usd, eur);
        await db.SaveChangesAsync();

        var service = new LedgerGrpcService(db, scope.ServiceProvider.GetRequiredService<IDailyPayoutQuery>());
        var response = await service.GetBalances(
            new GetBalancesRequest { MerchantId = merchantId.ToString(), Currency = "eur" }, null!);

        var balance = Assert.Single(response.Balances);
        Assert.Equal("EUR", balance.Currency);
        Assert.Equal(3000, balance.Pending);
    }

    [Fact]
    public async Task GetBalances_UnknownMerchant_ReturnsEmpty()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new LedgerGrpcService(db, scope.ServiceProvider.GetRequiredService<IDailyPayoutQuery>());

        var response = await service.GetBalances(
            new GetBalancesRequest { MerchantId = Guid.NewGuid().ToString() }, null!);

        Assert.Empty(response.Balances);
    }
}
