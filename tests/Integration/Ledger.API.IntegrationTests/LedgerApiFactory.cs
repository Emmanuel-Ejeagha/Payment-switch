using Ledger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentSwitch.IntegrationTests.Shared;
using Testcontainers.PostgreSql;

namespace Ledger.API.IntegrationTests;

public class LedgerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("LedgerDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    public LedgerApiFactory() => TestSecrets.ApplyEnvironment("LedgerService");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        TestSecrets.ApplyConnectionString("LedgerDb", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
