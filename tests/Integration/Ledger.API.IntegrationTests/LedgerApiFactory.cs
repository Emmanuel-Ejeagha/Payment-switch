using Ledger.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentSwitch.IntegrationTests.Shared;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Ledger.API.IntegrationTests;

public class LedgerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("LedgerDb")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername(TestSecrets.RabbitMqUserName)
        .WithPassword(TestSecrets.RabbitMqPassword)
        .Build();

    public LedgerApiFactory() => TestSecrets.ApplyEnvironment("LedgerService");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();
        TestSecrets.ApplyConnectionString("LedgerDb", _postgres.GetConnectionString());
        TestSecrets.ApplyRabbitMqHost(_rabbitMq.Hostname, _rabbitMq.GetMappedPublicPort(5672));
        await DeclarePaymentEventsExchangeAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// The <c>payment.events</c> exchange is normally declared by the Payment API
    /// at startup. The Ledger consumer binds its queue to it, so the exchange must
    /// exist before the API host starts or the consumer's first bind attempt fails
    /// and it falls into its 10s reconnect loop. Declaring it here (idempotent)
    /// makes the consumer connect on the first attempt.
    /// </summary>
    private async Task DeclarePaymentEventsExchangeAsync()
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
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
    }
}
