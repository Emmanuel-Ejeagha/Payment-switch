using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Payment.Infrastructure.Persistence;
using PaymentSwitch.IntegrationTests.Shared;
using Testcontainers.PostgreSql;

namespace Payment.API.IntegrationTests;

public class PaymentApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("PaymentDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    public PaymentApiFactory() => TestSecrets.ApplyEnvironment("PaymentService");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        TestSecrets.ApplyConnectionString("PaymentDb", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
