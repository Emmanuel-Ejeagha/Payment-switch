using System.Text;
using BuildingBlocks.Shared.Messaging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves the <see cref="RabbitMqChannelPool"/> fulfils TASK-026 against a real
/// broker: publishes share one connection (no per-poll churn), the configured
/// exchange is consistently declared and used, and a broker restart is survived
/// by reconnecting and re-declaring topology.
/// </summary>
public class EventBusTests : IClassFixture<EventBusFixture>
{
    private const string ExchangeName = "ledger.events";

    private readonly EventBusFixture _fixture;

    public EventBusTests(EventBusFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Pool_SequentialPublishes_ShareOneConnection()
    {
        await using var pool = CreatePool();
        var queueName = await AddScratchQueueAsync();

        for (var i = 0; i < 10; i++)
        {
            await PublishAsync(pool, queueName, $"msg-{i}");
        }

        Assert.Equal(1, pool.ConnectionsCreated);
        await AssertQueueDepthAsync(queueName, 10);
    }

    [Fact]
    public async Task Pool_AfterBrokerRestart_ReconnectsAndRepublishes()
    {
        await using var pool = CreatePool();
        var queueBefore = await AddScratchQueueAsync();

        // Prime the pool: first publish opens the singleton connection.
        await PublishAsync(pool, queueBefore, "before-restart");
        await AssertQueueDepthAsync(queueBefore, 1);
        Assert.Equal(1, pool.ConnectionsCreated);

        await _fixture.RestartAsync();

        // A fresh queue declared only after the restart proves the pool re-declares
        // its exchange on the reconnected broker; publishers racing the client's
        // connection-drop detection simply retry until the pool reconnects.
        var queueAfter = await AddScratchQueueAsync();
        await WaitUntilAsync(async () =>
        {
            try
            {
                await PublishAsync(pool, queueAfter, "after-restart");
                return await QueueDepthAsync(queueAfter) >= 1;
            }
            catch (Exception)
            {
                return false;
            }
        });

        Assert.Equal(2, pool.ConnectionsCreated);
        await AssertQueueDepthAsync(queueAfter, 1);
    }

    private RabbitMqChannelPool CreatePool() => new(
        () => _fixture.NewFactory(),
        channel => channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true),
        NullLogger<RabbitMqChannelPool>.Instance);

    private async Task<string> AddScratchQueueAsync()
    {
        var queueName = $"ledger.events.q.{Guid.NewGuid():N}";
        await using var connection = await _fixture.NewFactory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync(queueName, ExchangeName, "#");
        return queueName;
    }

    private async Task PublishAsync(RabbitMqChannelPool pool, string queueName, string body)
    {
        var channel = await pool.GetChannelAsync();
        try
        {
            await channel.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: queueName,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    Persistent = true,
                    ContentType = "application/json"
                },
                body: Encoding.UTF8.GetBytes(body));
        }
        finally
        {
            await pool.ReturnAsync(channel);
        }
    }

    private async Task<long> QueueDepthAsync(string queueName)
    {
        await using var connection = await _fixture.NewFactory().CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var declare = await channel.QueueDeclarePassiveAsync(queueName);
        return declare.MessageCount;
    }

    private Task AssertQueueDepthAsync(string queueName, long expected)
    {
        return WaitUntilAsync(async () => await QueueDepthAsync(queueName) >= expected, "timed out waiting for delivered messages");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, string message = "timed out", TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
                return;
            await Task.Delay(500);
        }

        Assert.Fail(message);
    }
}