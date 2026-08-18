extern alias IdentityApi;
extern alias MerchantApi;
extern alias PaymentApi;
extern alias LedgerApi;
extern alias NotificationApi;
extern alias SettlementApi;

using BuildingBlocks.Shared.Email;
using E2E.IntegrationTests.Support;
using IdentityProgram = IdentityApi::Program;
using LedgerProgram = LedgerApi::Program;
using MerchantProgram = MerchantApi::Program;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Notification.Application.Interfaces;
using NotificationProgram = NotificationApi::Program;
using Npgsql;
using Payment.Application.Interfaces;
using PaymentProgram = PaymentApi::Program;
using PaymentSwitch.IntegrationTests.Shared;
using RabbitMQ.Client;
using Settlement.Application.Interfaces;
using SettlementProgram = SettlementApi::Program;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace E2E.IntegrationTests;

/// <summary>
/// Runs all six services as in-memory <see cref="WebApplicationFactory{TEntryPoint}"/>
/// hosts over one shared Postgres container (one database per service) and one
/// shared RabbitMQ container. Broker/DB-backed flows (outbox, consumers, inbox,
/// DLQ) work end-to-end; only the cross-service gRPC lookups and SMTP are
/// replaced by test doubles.
/// </summary>
public class E2EFactory : IAsyncLifetime
{
    private static readonly string[] Databases =
    {
        "IdentityDb", "MerchantDb", "PaymentDb", "LedgerDb", "NotificationDb", "SettlementDb"
    };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("postgres")
        .WithUsername("paymentswitch")
        .WithPassword(TestSecrets.PostgresPassword)
        .Build();

    public RabbitMqContainer RabbitMq { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername(TestSecrets.RabbitMqUserName)
        .WithPassword(TestSecrets.RabbitMqPassword)
        .Build();

    public CapturingEmailSender Emails { get; } = new();

    public WebApplicationFactory<IdentityProgram> IdentityHost { get; private set; } = null!;
    public WebApplicationFactory<MerchantProgram> MerchantHost { get; private set; } = null!;
    public WebApplicationFactory<PaymentProgram> PaymentHost { get; private set; } = null!;
    public WebApplicationFactory<LedgerProgram> LedgerHost { get; private set; } = null!;
    public WebApplicationFactory<NotificationProgram> NotificationHost { get; private set; } = null!;
    public WebApplicationFactory<SettlementProgram> SettlementHost { get; private set; } = null!;

    public string MerchantDbConnectionString { get; private set; } = string.Empty;
    public string LedgerDbConnectionString { get; private set; } = string.Empty;
    public string NotificationDbConnectionString { get; private set; } = string.Empty;

    public E2EFactory()
    {
        TestSecrets.ApplyEnvironment("IdentityService");
        Environment.SetEnvironmentVariable("Seed__AdminPassword", "integration-test-admin-password");
        Emails.Reset();
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await RabbitMq.StartAsync();

        await CreateDatabasesAsync();

        TestSecrets.ApplyConnectionString("IdentityDb", ConnectionStringFor("IdentityDb"));
        MerchantDbConnectionString = ConnectionStringFor("MerchantDb");
        TestSecrets.ApplyConnectionString("MerchantDb", MerchantDbConnectionString);
        TestSecrets.ApplyConnectionString("PaymentDb", ConnectionStringFor("PaymentDb"));
        LedgerDbConnectionString = ConnectionStringFor("LedgerDb");
        TestSecrets.ApplyConnectionString("LedgerDb", LedgerDbConnectionString);
        NotificationDbConnectionString = ConnectionStringFor("NotificationDb");
        TestSecrets.ApplyConnectionString("NotificationDb", NotificationDbConnectionString);
        TestSecrets.ApplyConnectionString("SettlementDb", ConnectionStringFor("SettlementDb"));

        TestSecrets.ApplyRabbitMqHost(RabbitMq.Hostname, RabbitMq.GetMappedPublicPort(5672));
        await DeclarePaymentEventsExchangeAsync();

        IdentityHost = BuildIdentityHost();
        MerchantHost = new WebApplicationFactory<MerchantProgram>();
        PaymentHost = BuildPaymentHost();
        LedgerHost = new WebApplicationFactory<LedgerProgram>();
        NotificationHost = BuildNotificationHost();
        SettlementHost = BuildSettlementHost();

        // Force all six hosts to build (config validation, migrations, admin seed,
        // RabbitMQ consumers) before any test runs.
        _ = IdentityHost.Server;
        _ = MerchantHost.Server;
        _ = PaymentHost.Server;
        _ = LedgerHost.Server;
        _ = NotificationHost.Server;
        _ = SettlementHost.Server;
    }

    public async Task DisposeAsync()
    {
        await IdentityHost.DisposeAsync();
        await MerchantHost.DisposeAsync();
        await PaymentHost.DisposeAsync();
        await LedgerHost.DisposeAsync();
        await NotificationHost.DisposeAsync();
        await SettlementHost.DisposeAsync();
        await RabbitMq.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private WebApplicationFactory<IdentityProgram> BuildIdentityHost()
        => new WebApplicationFactory<IdentityProgram>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            }));

    private WebApplicationFactory<PaymentProgram> BuildPaymentHost()
        => new WebApplicationFactory<PaymentProgram>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMerchantService>();
                services.AddScoped<IMerchantService>(_ => new DbBackedMerchantService(MerchantDbConnectionString));
            }));

    private WebApplicationFactory<NotificationProgram> BuildNotificationHost()
        => new WebApplicationFactory<NotificationProgram>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IMerchantContactService>();
                services.AddScoped<IMerchantContactService>(_ => new DbBackedMerchantContactService(MerchantDbConnectionString));
            }));

    private WebApplicationFactory<SettlementProgram> BuildSettlementHost()
        => new WebApplicationFactory<SettlementProgram>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILedgerService>();
                services.AddScoped<ILedgerService>(_ => new DbBackedLedgerService(LedgerDbConnectionString));
            }));

    private async Task CreateDatabasesAsync()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        foreach (var database in Databases)
        {
            await using var command = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private string ConnectionStringFor(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = database
        };
        return builder.ConnectionString;
    }

    /// <summary>
    /// The <c>payment.events</c> exchange is normally declared by the Payment API
    /// at startup. The Ledger and Notification consumers bind their queues to it,
    /// so the exchange must exist before the hosts start or the consumers' first
    /// bind attempts fail and they fall into their reconnect loops. Declaring it
    /// here (idempotent) lets them connect on the first attempt.
    /// </summary>
    private async Task DeclarePaymentEventsExchangeAsync()
    {
        var factory = new ConnectionFactory
        {
            HostName = RabbitMq.Hostname,
            Port = RabbitMq.GetMappedPublicPort(5672),
            UserName = TestSecrets.RabbitMqUserName,
            Password = TestSecrets.RabbitMqPassword
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync("payment.events", ExchangeType.Topic, durable: true);
    }
}