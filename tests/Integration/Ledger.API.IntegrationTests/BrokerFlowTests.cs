using System.Text;
using System.Text.Json;
using Ledger.Infrastructure.Messaging;
using Ledger.Infrastructure.Outbox;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves the broker-backed pipeline against a real RabbitMQ Testcontainer:
/// transactional outbox → exchange → consumer → inbox → DB posting.
/// </summary>
public class BrokerFlowTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public BrokerFlowTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Outbox_And_ConsumerFlow_WorkAgainstRealBroker()
    {
        // Leg 1 — outbox → exchange: enqueue via Ledger's transactional outbox and
        // let OutboxPublisherService publish it to the broker (Processed = true).
        Guid outboxId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var outbox = new OutboxMessage("FundsReservedEvent", """{"amount":5000}""");
            db.OutboxMessages.Add(outbox);
            await db.SaveChangesAsync();
            outboxId = outbox.Id;
        }

        await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.OutboxMessages.AnyAsync(m => m.Id == outboxId && m.Processed);
        });

        // Leg 2 — exchange → consumer → inbox → DB: publish a payment event to
        // payment.events exactly as the Payment service's outbox publisher does.
        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new PaymentAuthorizedEvent(
            Guid.NewGuid(), merchantId, new MoneyPayload(5000, "USD"), "auth-code", "gateway-ref"));

        await PublishToPaymentEventsAsync(messageId, payload);

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
            Assert.Equal("PaymentAuthorizedDomainEvent", inbox.EventType);
            Assert.NotNull(inbox.ProcessedAt);

            var account = await db.LedgerAccounts
                .Include(a => a.Journal)
                .FirstOrDefaultAsync(a => a.MerchantId == merchantId);
            Assert.NotNull(account);
            Assert.Equal("USD", account!.Currency);
            Assert.Equal(5000, account.PendingBalance);
            Assert.Equal(5000, account.ReservedBalance);
            Assert.Equal(0, account.AvailableBalance);
            Assert.Contains(account.Journal, j => j.Description == "Funds reserved" && j.Amount.Amount == 5000);
        }
    }

    private async Task PublishToPaymentEventsAsync(string messageId, string payload)
    {
        using var scope = _factory.Services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMQSettings>>();
        var factory = new ConnectionFactory
        {
            HostName = settings.Value.HostName,
            Port = settings.Value.Port,
            UserName = settings.Value.UserName,
            Password = settings.Value.Password
        };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = "corr-" + messageId
        };
        await channel.BasicPublishAsync(
            exchange: "payment.events",
            routingKey: "PaymentAuthorizedDomainEvent",
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

        Assert.Fail("Timed out waiting for broker flow to complete.");
    }
}
