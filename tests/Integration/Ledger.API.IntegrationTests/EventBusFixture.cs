using PaymentSwitch.IntegrationTests.Shared;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Standalone RabbitMQ Testcontainer used by <see cref="EventBusTests"/> to prove
/// the shared <see cref="BuildingBlocks.Shared.Messaging.RabbitMqChannelPool"/>
/// keeps a single connection and survives a broker restart. No Postgres or API
/// host is needed, so the tests can stop/start the broker freely.
/// </summary>
public class EventBusFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.13-alpine")
        .WithUsername(TestSecrets.RabbitMqUserName)
        .WithPassword(TestSecrets.RabbitMqPassword)
        .Build();

    private string HostName => _rabbitMq.Hostname;
    private int Port => _rabbitMq.GetMappedPublicPort(5672);

    public async Task InitializeAsync()
    {
        await _rabbitMq.StartAsync();
        await WaitUntilBrokerReachableAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMq.DisposeAsync();
    }

    public ConnectionFactory NewFactory() => new()
    {
        HostName = HostName,
        Port = Port,
        UserName = TestSecrets.RabbitMqUserName,
        Password = TestSecrets.RabbitMqPassword,
        AutomaticRecoveryEnabled = false
    };

    /// <summary>Stops and restarts the broker container, waiting until it accepts connections again.</summary>
    public async Task RestartAsync()
    {
        await _rabbitMq.StopAsync();
        await _rabbitMq.StartAsync();
        await WaitUntilBrokerReachableAsync();
    }

    private async Task WaitUntilBrokerReachableAsync(TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(90));
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var connection = await NewFactory().CreateConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();
                await channel.ExchangeDeclareAsync("payment.events", ExchangeType.Topic, durable: true);
                return;
            }
            catch
            {
                await Task.Delay(500);
            }
        }

        throw new TimeoutException("RabbitMQ broker did not become reachable in time.");
    }
}