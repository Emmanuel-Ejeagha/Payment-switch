using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Persistence;
using PaymentSwitch.IntegrationTests.Shared;
using Testcontainers.PostgreSql;

namespace Notification.API.IntegrationTests;

public class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("NotificationDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    public NotificationApiFactory() => TestSecrets.ApplyEnvironment("NotificationService");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        TestSecrets.ApplyConnectionString("NotificationDb", _postgres.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
