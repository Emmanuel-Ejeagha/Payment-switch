using Microsoft.EntityFrameworkCore;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

using Notification.Domain.ValueObjects;

namespace Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(60);

    public NotificationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task AddAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(notification, cancellationToken);
    }

    public Task UpdateAsync(NotificationEntity notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(notification);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> due pending
    /// notifications by stamping a per-worker lease token, so concurrent worker
    /// instances never pick the same row (no duplicate sends). Rows whose lease
    /// has expired are re-claimable, which keeps retries moving after a crash.
    /// </summary>
    public async Task<List<NotificationEntity>> GetPendingForRetryAsync(DateTime now, int batchSize, CancellationToken cancellationToken = default)
    {
        var leaseToken = Guid.NewGuid();
        var leaseUntil = now.Add(LeaseDuration);

        var claimable = _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending
                && (n.NextRetryAt == null || n.NextRetryAt <= now)
                && (n.LeaseExpiresAt == null || n.LeaseExpiresAt < now))
            .OrderBy(n => n.NextRetryAt)
            .Select(n => n.Id)
            .Take(batchSize);

        await _context.Notifications
            .Where(n => claimable.Contains(n.Id))
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.LeaseToken, leaseToken)
                      .SetProperty(n => n.LeaseExpiresAt, leaseUntil),
                cancellationToken);

        return await _context.Notifications
            .Where(n => n.LeaseToken == leaseToken)
            .OrderBy(n => n.NextRetryAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationDto>> ListAsync(string? recipient, string? channel, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrEmpty(recipient))
            query = query.Where(n => n.Recipient.Contains(recipient));
        if (!string.IsNullOrEmpty(channel) && channel.ToLowerInvariant() is "email" or "sms" or "webhook")
            query = query.Where(n => n.Channel == NotificationChannel.FromString(channel));
        if (!string.IsNullOrEmpty(status) && status.ToLowerInvariant() is "pending" or "sent" or "failed")
            query = query.Where(n => n.Status == NotificationStatus.FromString(status));

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return notifications.Select(n => new NotificationDto(
            n.Id, n.Recipient, n.Channel.Value, n.Subject, n.Body,
            n.WebhookUrl, n.Status.Value, n.RetryCount,
            n.NextRetryAt, n.CreatedAt, n.ProcessedAt)).ToList();
    }
}