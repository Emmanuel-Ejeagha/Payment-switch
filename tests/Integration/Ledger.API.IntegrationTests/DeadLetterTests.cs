using System.Text;
using System.Text.Json;
using BuildingBlocks.Shared.Messaging;
using Ledger.Infrastructure.Messaging;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves the DLQ pipeline against a real RabbitMQ Testcontainer: a message that
/// exhausted its retries is routed to the dead-letter queue, consumed, and
/// persisted (never silently dropped).
/// </summary>
public class DeadLetterTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public DeadLetterTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExhaustedMessage_LandsInDlq_AndIsRecorded()
    {
        var messageId = Guid.NewGuid().ToString();
        const string payload = """{"amount":5000,"currency":"USD"}""";

        await PublishToDlqAsync(messageId, payload);

        await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.DeadLetterRecords.AnyAsync(r => r.MessageId == messageId);
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var record = await db.DeadLetterRecords.FirstAsync(r => r.MessageId == messageId);

            Assert.Equal("PaymentAuthorizedDomainEvent", record.EventType);
            Assert.Equal("ledger.payment.events.dlq", record.Queue);
            Assert.Equal("retries-exhausted", record.Reason);
            Assert.Equal(MessageRetryPolicy.MaxRetries, record.RetryCount);
            AssertJsonEqual(payload, record.Payload);
        }
    }

    private async Task PublishToDlqAsync(string messageId, string payload)
    {
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMQSettings>>();
        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            Port = settings.Value.Port,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        // Declaring the exchange is idempotent; ensures it exists even if the
        // consumer's declarer has not connected yet.
        await channel.ExchangeDeclareAsync("ledger.payment.events.dlx", ExchangeType.Topic, durable: true);

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
            exchange: "ledger.payment.events.dlx",
            routingKey: "PaymentAuthorizedDomainEvent",
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload));
    }

    private static void AssertJsonEqual(string expected, string actual)
    {
        // The payload column is jsonb; Postgres canonicalizes whitespace on
        // storage, so compare structurally rather than byte-for-byte.
        using var expectedDoc = JsonDocument.Parse(expected);
        using var actualDoc = JsonDocument.Parse(actual);
        Assert.True(
            JsonElement.DeepEquals(expectedDoc.RootElement, actualDoc.RootElement),
            $"Payloads differ structurally. Expected: {expected} Actual: {actual}");
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
                return;
            await Task.Delay(500);
        }

        Assert.Fail("Timed out waiting for dead-letter record.");
    }
}