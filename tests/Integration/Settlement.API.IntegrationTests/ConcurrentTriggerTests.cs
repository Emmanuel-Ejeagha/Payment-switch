using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Settlement.Application.Features.Command.TriggerSettlement;
using Settlement.Application.Interfaces;
using Settlement.Infrastructure.Persistence;
using Settlement.Infrastructure.Services;

namespace Settlement.API.IntegrationTests;

/// <summary>
/// Proves the unique BatchDate backstop: concurrent settlement triggers for the
/// same date must yield exactly one batch and both callers see its id.
/// </summary>
public class ConcurrentTriggerTests : IClassFixture<SettlementApiFactory>
{
    private readonly SettlementApiFactory _factory;

    public ConcurrentTriggerTests(SettlementApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentTriggers_ForSameDate_ProduceOneBatch()
    {
        using var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ILedgerService>();
                services.AddSingleton<ILedgerService>(new MockLedgerService());
            });
        });

        var date = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = new TriggerSettlementCommand(date);

        using var scope1 = host.Services.CreateScope();
        using var scope2 = host.Services.CreateScope();
        var handler1 = scope1.ServiceProvider.GetRequiredService<TriggerSettlementHandler>();
        var handler2 = scope2.ServiceProvider.GetRequiredService<TriggerSettlementHandler>();

        var results = await Task.WhenAll(handler1.Handle(command), handler2.Handle(command));

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(results[0].Value!.Id, results[1].Value!.Id);

        using var dbScope = host.Services.CreateScope();
        var db = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var batches = await db.SettlementBatches.CountAsync(b => b.BatchDate == date);
        Assert.Equal(1, batches);

        // Settlement totals equal the ledger daily sums (MockLedgerService returns
        // gross 500000+300000 and fees 10000+6000 USD → net total 784000).
        var batch = await db.SettlementBatches.FirstAsync(b => b.BatchDate == date);
        Assert.Equal(784000, batch.TotalAmount);
    }
}
