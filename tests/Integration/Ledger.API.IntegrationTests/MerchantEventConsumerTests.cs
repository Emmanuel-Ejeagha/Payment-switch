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
/// Proves the merchant lifecycle consumer against a real RabbitMQ Testcontainer:
/// a MerchantOnboardedEvent on <c>merchant.events</c> provisions a default
/// (USD) ledger account so funds can be reserved before the first payment.
/// </summary>
public class MerchantEventConsumerTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public MerchantEventConsumerTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MerchantOnboardedEvent_ProvisionsDefaultCurrencyLedgerAccount()
    {
        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new { MerchantId = merchantId });

        await PublishToMerchantEventsAsync(messageId, payload);

        await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ProcessedAt != null);
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var inbox = await db.InboxMessages.FirstAsync(m => m.MessageId == messageId);
            Assert.Equal("MerchantOnboardedEvent", inbox.EventType);

            var account = await db.LedgerAccounts.FirstOrDefaultAsync(a => a.MerchantId == merchantId);
            Assert.NotNull(account);
            Assert.Equal("USD", account!.Currency);
            Assert.Equal(0, account.PendingBalance);
            Assert.Equal(0, account.ReservedBalance);
            Assert.Equal(0, account.AvailableBalance);
        }
    }

    private async Task PublishToMerchantEventsAsync(string messageId, string payload)
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

        // Declaring the exchange/queue/binding is idempotent and matches what
        // MerchantEventConsumerService declares on connect. Ensures the published
        // message is routed even if the consumer has not connected yet.
        await channel.ExchangeDeclareAsync("merchant.events", ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync("ledger.merchant.events", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync("ledger.merchant.events", "merchant.events", "MerchantOnboardedEvent");

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = "corr-" + messageId
        };
        await channel.BasicPublishAsync(
            exchange: "merchant.events",
            routingKey: "MerchantOnboardedEvent",
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(payload));
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

        Assert.Fail("Timed out waiting for merchant event to be processed.");
    }
}
