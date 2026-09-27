using BuildingBlocks.Shared.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Notification.Application.Interfaces;
using Notification.Domain.ValueObjects;
using Notification.Infrastructure.Messaging;
using Notification.Infrastructure.Persistence;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.API.IntegrationTests;

/// <summary>
/// Reproduces two real worker behaviors against a real Postgres:
/// (a) concurrent worker instances must never pick the same pending
/// notification (no duplicate sends), and (b) the consumer restores the
/// originating request's correlation ID into its async flow.
/// </summary>
public class NotificationLeaseTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;
    private readonly DateTime _now = DateTime.UtcNow;

    public NotificationLeaseTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentWorkers_ClaimDisjointSets_NoDuplicateSends()
    {
        var due = await SeedAsync(_now.AddMinutes(-1), leaseExpiresAt: null, count: 6);

        // Two worker instances running the original query both returned the same
        // rows; with the lease claim each row is picked by exactly one worker.
        var workerA = ClaimAsync();
        var workerB = ClaimAsync();
        await Task.WhenAll(workerA, workerB);

        var pickedByA = await workerA;
        var pickedByB = await workerB;

        Assert.Empty(pickedByA.Intersect(pickedByB));
        Assert.Equal(due.Count, pickedByA.Concat(pickedByB).Distinct().Count());
    }

    [Fact]
    public async Task ActiveLease_IsNotReclaimed()
    {
        // A row already held by another worker (active lease) must stay put.
        await SeedAsync(_now.AddMinutes(-1), leaseExpiresAt: _now.AddMinutes(1), count: 2);

        var picked = await ClaimAsync();

        Assert.Empty(picked);
    }

    [Fact]
    public async Task ExpiredLease_IsReClaimable()
    {
        // A worker that crashed mid-send holds a lease that expires, freeing the
        // row for re-pickup so the notification is not lost forever.
        var (id, _) = (await SeedAsync(_now.AddMinutes(-1), leaseExpiresAt: _now.AddSeconds(-10), count: 1)).Single();

        var picked = await ClaimAsync();

        Assert.Equal(new[] { id }, picked);
    }

    [Fact]
    public async Task FreshNotification_WithNullNextRetryAt_IsPickedImmediately()
    {
        // Freshly created notifications have NextRetryAt == null and are due now;
        // the repo must not treat NULL as "never due".
        var (id, _) = (await SeedAsync(null, leaseExpiresAt: null, count: 1)).Single();

        var picked = await ClaimAsync();

        Assert.Equal(new[] { id }, picked);
    }

    [Fact]
    public async Task Consumer_RestoresCorrelationIntoAsyncFlow()
    {
        var settings = _factory.Services.GetRequiredService<IOptions<RabbitMQSettings>>();
        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var provider = _factory.Services.GetRequiredService<ICorrelationIdProvider>();
        var logger = _factory.Services.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RabbitMQConsumerService>>();

        var consumer = new RabbitMQConsumerService(settings, scopeFactory, provider, logger);

        consumer.RestoreCorrelation("corr-987");

        Assert.Equal("corr-987", provider.CorrelationId);

        // Blank headers must not clobber an already-restored correlation.
        consumer.RestoreCorrelation(" ");
        Assert.Equal("corr-987", provider.CorrelationId);
    }

    private async Task<List<Guid>> ClaimAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var claimed = await repo.GetPendingForRetryAsync(DateTime.UtcNow, 10);
        return claimed.Select(n => n.Id).ToList();
    }

    /// <summary>
    /// Seeds <paramref name="count"/> pending notifications with the given due
    /// time and lease state, returning (Id, Recipient) pairs.
    /// </summary>
    private async Task<List<(Guid Id, string Recipient)>> SeedAsync(DateTime? nextRetryAt, DateTime? leaseExpiresAt, int count)
    {
        var seeded = new List<(Guid Id, string Recipient)>();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var i = 0; i < count; i++)
        {
            var notification = new NotificationEntity(
                Guid.NewGuid(),
                $"merchant{i}@example.com",
                NotificationChannel.Email,
                "Subject",
                "Body",
                null,
                "{}");
            notification.NextRetryAt = nextRetryAt;
            if (leaseExpiresAt.HasValue)
            {
                notification.LeaseToken = Guid.NewGuid();
                notification.LeaseExpiresAt = leaseExpiresAt;
            }
            db.Notifications.Add(notification);
            seeded.Add((notification.Id, notification.Recipient));
        }

        await db.SaveChangesAsync();
        return seeded;
    }
}