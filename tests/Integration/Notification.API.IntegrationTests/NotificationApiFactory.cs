using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Interfaces;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Services;
using PaymentSwitch.IntegrationTests.Shared;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Notification.API.IntegrationTests;

public class NotificationApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("NotificationDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername(TestSecrets.RabbitMqUserName)
        .WithPassword(TestSecrets.RabbitMqPassword)
        .Build();

    public NotificationApiFactory() => TestSecrets.ApplyEnvironment("NotificationService");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // In tests no Merchant service runs, so its gRPC contact lookup can
            // never resolve a recipient. Stub it so the notification-creation
            // flow can be exercised end-to-end.
            services.AddScoped<IMerchantContactService, StubMerchantContactService>();

            // The sender polls every 5s and claims pending rows for itself; it
            // races the lease tests' explicit claims (seeded rows vanish under
            // it on slow hosts). Lease/broker behavior is driven explicitly in
            // tests, so the background sender stays off here.
            var sender = services.FirstOrDefault(d =>
                d.ImplementationType == typeof(NotificationSenderBackgroundService));
            if (sender is not null)
                services.Remove(sender);
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        TestSecrets.ApplyConnectionString("NotificationDb", _postgres.GetConnectionString());
        TestSecrets.ApplyRabbitMqHost(_rabbitMq.Hostname, _rabbitMq.GetMappedPublicPort(5672));
        await DeclareSourceExchangesAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// The <c>payment.events</c> (Payment API) and <c>identity.events</c> (Identity
    /// API) exchanges are normally declared by their producers at startup. The
    /// Notification consumer binds its queue to both, so the exchanges must exist
    /// before the API host starts or the consumer's first bind attempt fails and
    /// it falls into its 10s reconnect loop. Declaring them here (idempotent)
    /// makes the consumer connect on the first attempt.
    /// </summary>
    private async Task DeclareSourceExchangesAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMq.Hostname,
            Port = _rabbitMq.GetMappedPublicPort(5672),
            UserName = TestSecrets.RabbitMqUserName,
            Password = TestSecrets.RabbitMqPassword
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync("payment.events", ExchangeType.Topic, durable: true);
        await channel.ExchangeDeclareAsync("identity.events", ExchangeType.Topic, durable: true);
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }

    private sealed class StubMerchantContactService : IMerchantContactService
    {
        public Task<string?> GetMerchantEmailAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>($"merchant-{merchantId}@example.com");
    }
}