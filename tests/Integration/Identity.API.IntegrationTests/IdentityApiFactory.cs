using BuildingBlocks.Shared.Email;
using Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    /// <summary>
    /// Captures the transactional emails the API would dispatch (verification,
    /// password reset) instead of sending them, so the single-use plaintext
    /// tokens can be driven through the real endpoints end-to-end.
    /// </summary>
    public static readonly CapturingEmailSender Emails = new();

    public IdentityApiFactory()
    {
        TestSecrets.ApplyEnvironment("IdentityService");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", "integration-test-admin-password");
        Emails.Reset();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Emails);
        });
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
