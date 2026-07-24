using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Notification.API.IntegrationTests;

public class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("NotificationDb")
        .WithUsername("paymentswitch")
        .WithPassword("paymentswitch")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:NotificationDb"] = _postgres.GetConnectionString(),
                ["Jwt:Secret"] = "test-super-secret-key-minimum-32-bytes!!",
                ["Jwt:Issuer"] = "NotificationService",
                ["Jwt:Audience"] = "PaymentSwitch"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
