using System.Text;
using System.Text.Json;
using BuildingBlocks.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Notification.API.IntegrationTests;

/// <summary>
/// Proves the DLQ pipeline against a real RabbitMQ Testcontainer: a message that
/// exhausted its retries is routed to the dead-letter queue, consumed, and
/// persisted (never silently dropped).
/// </summary>
public class DeadLetterTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public DeadLetterTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExhaustedMessage_LandsInDlq_AndIsRecorded()
    {
        var messageId = Guid.NewGuid().ToString();
        const string payload = """{"event":"PaymentCaptured","amount":5000}""";

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

            Assert.Equal("PaymentCapturedDomainEvent", record.EventType);
            Assert.Equal("notification.events.dlq", record.Queue);
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
        await channel.ExchangeDeclareAsync("notification.events.dlx", ExchangeType.Topic, durable: true);

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
            exchange: "notification.events.dlx",
            routingKey: "PaymentCapturedDomainEvent",
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