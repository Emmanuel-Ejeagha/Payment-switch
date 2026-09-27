using BuildingBlocks.Shared.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notification.Domain.ValueObjects;
using Notification.Infrastructure.Inbox;
using Notification.Infrastructure.Outbox;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Retention;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.API.IntegrationTests;

/// <summary>
/// TASK-042: the Notification retention sweep archives delivered notifications
/// (90 days) and processed inbox/outbox messages (7d) into
/// <c>ArchivedRecords</c>; pending/unsent notifications are never touched.
/// </summary>
public class RetentionTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public RetentionTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeliveredNotification_Expired_IsArchivedAndRemoved_FreshStays()
    {
        var oldId = Guid.NewGuid();
        var freshId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var old = new NotificationEntity(oldId, "old@example.com", NotificationChannel.Email,
                "Subject", "Body", null, """{"amount":5000}""");
            old.MarkAsSent();
            var fresh = new NotificationEntity(freshId, "fresh@example.com", NotificationChannel.Email,
                "Subject", "Body", null, """{"amount":100}""");
            fresh.MarkAsSent();
            var pending = new NotificationEntity(Guid.NewGuid(), "pending@example.com", NotificationChannel.Email,
                "Subject", "Body", null, "{}");
            db.Notifications.Add(old);
            db.Notifications.Add(fresh);
            db.Notifications.Add(pending);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Notifications\" SET \"ProcessedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-120), oldId);
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await vdb.Notifications.FirstOrDefaultAsync(n => n.Id == oldId));
        Assert.NotNull(await vdb.Notifications.FirstOrDefaultAsync(n => n.Id == freshId));
        Assert.NotNull(await vdb.Notifications.FirstOrDefaultAsync(n => n.Recipient == "pending@example.com"));

        var archivedNotifications = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "Notifications")
            .ToListAsync();
        var archive = archivedNotifications.SingleOrDefault(r => r.Payload.Contains(oldId.ToString()));
        Assert.NotNull(archive);
        Assert.Contains("old@example.com", archive!.Payload);
    }

    [Fact]
    public async Task InboxAndOutbox_ProcessedAndExpired_AreArchivedAndRemoved()
    {
        InboxMessage inbox;
        OutboxMessage outbox;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            inbox = new InboxMessage(Guid.NewGuid().ToString(), "PaymentAuthorizedDomainEvent", """{"amount":5000}""");
            inbox.MarkAsProcessed();
            outbox = new OutboxMessage("NotificationSentEvent", """{"amount":5000}""");
            outbox.MarkAsProcessed();
            db.InboxMessages.Add(inbox);
            db.OutboxMessages.Add(outbox);
            await db.SaveChangesAsync();

            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"InboxMessages\" SET \"ProcessedAt\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-30), inbox.Id);
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"OutboxMessages\" SET \"OccurredOn\" = {0} WHERE \"Id\" = {1}",
                DateTime.UtcNow.AddDays(-30), outbox.Id);
        }

        await RunRetentionAsync();

        using var verify = _factory.Services.CreateScope();
        var vdb = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Null(await vdb.InboxMessages.FirstOrDefaultAsync(m => m.Id == inbox.Id));
        Assert.Null(await vdb.OutboxMessages.FirstOrDefaultAsync(m => m.Id == outbox.Id));

        var archivedInbox = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "InboxMessages")
            .ToListAsync();
        var archivedOutbox = await vdb.ArchivedRecords
            .Where(r => r.SourceTable == "OutboxMessages")
            .ToListAsync();
        Assert.NotNull(archivedInbox.SingleOrDefault(r => r.Payload.Contains(inbox.Id.ToString())));
        Assert.NotNull(archivedOutbox.SingleOrDefault(r => r.Payload.Contains(outbox.Id.ToString())));
    }

    private async Task RunRetentionAsync()
    {
        var service = _factory.Services.GetServices<Microsoft.Extensions.Hosting.IHostedService>()
            .OfType<NotificationRetentionService>().Single();
        await service.RunOnceAsync(CancellationToken.None);
    }
}