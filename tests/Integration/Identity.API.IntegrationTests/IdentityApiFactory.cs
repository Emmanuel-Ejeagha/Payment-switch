using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentSwitch.IntegrationTests.Shared;
using Testcontainers.PostgreSql;

namespace Identity.API.IntegrationTests;

public class IdentityApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("IdentityDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    public IdentityApiFactory()
    {
        TestSecrets.ApplyEnvironment("IdentityService");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", "integration-test-admin-password");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        TestSecrets.ApplyConnectionString("IdentityDb", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.StopAsync();
        await _postgres.DisposeAsync();
    }
}
