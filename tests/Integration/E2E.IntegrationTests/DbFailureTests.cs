using System.Text;
using System.Text.Json;
using BuildingBlocks.Shared.Messaging;
using Ledger.Infrastructure.Messaging;
using LedgerAppDbContext = Ledger.Infrastructure.Persistence.AppDbContext;
using Microsoft.EntityFrameworkCore;
using PaymentSwitch.IntegrationTests.Shared;
using RabbitMQ.Client;

namespace E2E.IntegrationTests;

/// <summary>
/// Failure-path test: a payment event that the Ledger cannot process (unknown
/// merchant, no account) exhausts its retries and lands in the dead-letter
/// queue, where the DLQ consumer persists it — nothing is silently dropped.
/// </summary>
public class DbFailureTests : IClassFixture<E2EFactory>
{
    private readonly E2EFactory _factory;

    public DbFailureTests(E2EFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CapturedEventForUnknownMerchant_EndsInDeadLetterQueue()
    {
        var messageId = Guid.NewGuid().ToString("N");
        var payload = new PaymentCapturedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(), // merchant with no ledger account -> CaptureFunds fails
            new MoneyPayload(1000, "USD"),
            Guid.NewGuid());

        await PublishAsync("payment.events", "PaymentCapturedDomainEvent", messageId, JsonSerializer.Serialize(payload));

        await WaitUntilAsync(async () =>
        {
            await using var db = new LedgerAppDbContext(DbOptions());
            return await db.DeadLetterRecords.AnyAsync(r => r.MessageId == messageId);
        });

        await using var verifyDb = new LedgerAppDbContext(DbOptions());
        var record = await verifyDb.DeadLetterRecords.FirstAsync(r => r.MessageId == messageId);
        Assert.Equal("PaymentCapturedDomainEvent", record.EventType);
        Assert.Equal("ledger.payment.events.dlq", record.Queue);
        Assert.Equal("retries-exhausted", record.Reason);
        Assert.Equal(MessageRetryPolicy.MaxRetries, record.RetryCount);
    }

    private async Task PublishAsync(string exchange, string routingKey, string messageId, string payload)
    {
        var factory = new ConnectionFactory
        {
            HostName = _factory.RabbitMq.Hostname,
            Port = _factory.RabbitMq.GetMappedPublicPort(5672),
            UserName = TestSecrets.RabbitMqUserName,
            Password = TestSecrets.RabbitMqPassword
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            Headers = new Dictionary<string, object?>
            {
                [MessageRetryPolicy.RetryCountHeader] = (long)MessageRetryPolicy.MaxRetries
            }
        };
        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload));
    }

    private DbContextOptions<LedgerAppDbContext> DbOptions()
        => new DbContextOptionsBuilder<LedgerAppDbContext>().UseNpgsql(_factory.LedgerDbConnectionString).Options;

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate)
    {
        var deadline = DateTime.UtcNow.Add(TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
                return;
            await Task.Delay(500);
        }

        Assert.Fail("Timed out waiting for dead-letter record.");
    }
}