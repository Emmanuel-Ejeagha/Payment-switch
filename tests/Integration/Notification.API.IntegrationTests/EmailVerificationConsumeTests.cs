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
/// Proves the verification-event path against a real RabbitMQ Testcontainer: an
/// <c>EmailVerificationRequestedDomainEvent</c> on <c>identity.events</c> becomes
/// a persisted Resend-routed notification (no merchant lookup, no preference
/// gating), and duplicate delivery creates nothing twice.
/// </summary>
public class EmailVerificationConsumeTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public EmailVerificationConsumeTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task VerificationRequestedEvent_CreatesResendNotification()
    {
        var email = $"verify-{Guid.NewGuid():N}@example.com";
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new EmailVerificationRequestedEvent(
            Guid.NewGuid(), email, "ABCDEF1234567890",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(24)));

        await PublishToIdentityEventsAsync("EmailVerificationRequestedDomainEvent", messageId, payload);

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
            Assert.Equal("EmailVerificationRequestedDomainEvent", inbox.EventType);

            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.Recipient == email);
            Assert.NotNull(notification);
            Assert.Equal("email", notification!.Channel.Value);
            Assert.Equal("resend", notification.Provider);
            Assert.Equal("Confirm your email address", notification.Subject);
            Assert.Contains("localhost:3000/verify-email?email=", notification.Body);
        }
    }

    [Fact]
    public async Task VerificationRequestedEvent_DuplicateDelivery_CreatesOneNotification()
    {
        var email = $"verify-dup-{Guid.NewGuid():N}@example.com";
        var messageId = Guid.NewGuid().ToString();
        var payload = JsonSerializer.Serialize(new EmailVerificationRequestedEvent(
            Guid.NewGuid(), email, "ABCDEF1234567890",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(24)));

        await PublishToIdentityEventsAsync("EmailVerificationRequestedDomainEvent", messageId, payload);
        await PublishToIdentityEventsAsync("EmailVerificationRequestedDomainEvent", messageId, payload);

        await WaitUntilAsync(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.InboxMessages.AnyAsync(m => m.MessageId == messageId && m.ProcessedAt != null);
        });

        // Let a potential double-processing settle, then assert idempotency:
        // the background sender is disabled in tests, so the count is stable.
        await Task.Delay(TimeSpan.FromSeconds(5));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(1, await db.Notifications.CountAsync(n => n.Recipient == email));
        }
    }

    private async Task PublishToIdentityEventsAsync(string routingKey, string messageId, string payload)
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

        await channel.ExchangeDeclareAsync("identity.events", ExchangeType.Topic, durable: true);
        await channel.QueueDeclareAsync("notification.events", durable: true, exclusive: false, autoDelete: false);
        await channel.QueueBindAsync("notification.events", "identity.events", "EmailVerificationRequestedDomainEvent");

        var properties = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = messageId,
            CorrelationId = "corr-" + messageId
        };
        await channel.BasicPublishAsync(
            exchange: "identity.events",
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

        Assert.Fail("Timed out waiting for the verification event to be consumed.");
    }
}
