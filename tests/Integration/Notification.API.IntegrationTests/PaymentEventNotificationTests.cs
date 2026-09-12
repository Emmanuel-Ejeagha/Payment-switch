using System.Text;
using System.Text.Json;
using BuildingBlocks.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notification.Application.Messaging;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using RabbitMQ.Client;

namespace Notification.API.IntegrationTests;

/// <summary>
/// Proves the consumer's notification-creation path against a real RabbitMQ
/// Testcontainer: a payment event on <c>payment.events</c> becomes a persisted
/// notification for the merchant's resolved contact email.
/// </summary>
public class PaymentEventNotificationTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public PaymentEventNotificationTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentIntentCreatedEvent_CreatesNotification()
    {
        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new PaymentIntentCreatedEvent(
            Guid.NewGuid(), merchantId, new MoneyPayload(1999, "USD"), "idem-key"));

        await PublishToPaymentEventsAsync("PaymentIntentCreatedDomainEvent", messageId, payload);

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
            Assert.Equal("PaymentIntentCreatedDomainEvent", inbox.EventType);

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Recipient == $"merchant-{merchantId}@example.com");
            Assert.NotNull(notification);
            Assert.Equal("Payment Started", notification!.Subject);
            Assert.Contains("1999", notification.Body);
            Assert.Contains("USD", notification.Body);
        }
    }

    [Fact]
    public async Task PaymentVoidedEvent_CreatesNotification()
    {
        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new PaymentVoidedEvent(Guid.NewGuid(), merchantId));

        await PublishToPaymentEventsAsync("PaymentVoidedDomainEvent", messageId, payload);

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
            Assert.Equal("PaymentVoidedDomainEvent", inbox.EventType);

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Recipient == $"merchant-{merchantId}@example.com");
            Assert.NotNull(notification);
            Assert.Equal("Payment Voided", notification!.Subject);
        }
    }

    [Fact]
    public async Task PaymentFailedEvent_CreatesNotification()
    {
        var merchantId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new PaymentFailedEvent(
            Guid.NewGuid(), merchantId, new MoneyPayload(10000, "USD")));

        await PublishToPaymentEventsAsync("PaymentFailedDomainEvent", messageId, payload);

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
            Assert.Equal("PaymentFailedDomainEvent", inbox.EventType);

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Recipient == $"merchant-{merchantId}@example.com");
            Assert.NotNull(notification);
            Assert.Equal("Payment Failed", notification!.Subject);
        }
    }

    private async Task PublishToPaymentEventsAsync(string routingKey, string messageId, string payload)
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

        // Idempotent declaration of the topology RabbitMQConsumerService sets up
        // on connect, so a published message is routed even if the consumer has
        // not connected yet.
        await channel.ExchangeDeclareAsync("payment.events", ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync("notification.events", durable: true, exclusive: false, autoDelete: false);
        foreach (var key in new[]
                 {
                     "PaymentAuthorizedDomainEvent",
                     "PaymentCapturedDomainEvent",
                     "PaymentRefundedDomainEvent",
                     "PaymentIntentCreatedDomainEvent",
                     "PaymentVoidedDomainEvent"
                 })
        {
            await channel.QueueBindAsync("notification.events", "payment.events", key);
        }

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = "corr-" + messageId
        };
        await channel.BasicPublishAsync(
            exchange: "payment.events",
            routingKey: routingKey,
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

        Assert.Fail("Timed out waiting for notification to be created.");
    }
}
