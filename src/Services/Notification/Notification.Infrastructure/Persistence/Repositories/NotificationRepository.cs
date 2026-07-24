using Microsoft.EntityFrameworkCore;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using NotificationEntity = Notification.Domain.Entities.Notification;

using Notification.Domain.ValueObjects;

namespace Notification.Infrastructure.Persistence.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _context;

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

    public async Task<List<NotificationEntity>> GetPendingForRetryAsync(DateTime now, int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatus.Pending && n.NextRetryAt <= now)
            .OrderBy(n => n.NextRetryAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationDto>> ListAsync(string? recipient, string? channel, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrEmpty(recipient))
            query = query.Where(n => n.Recipient.Contains(recipient));
        if (!string.IsNullOrEmpty(channel))
            query = query.Where(n => n.Channel.Value == channel);
        if (!string.IsNullOrEmpty(status))
            query = query.Where(n => n.Status.Value == status);

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id, n.Recipient, n.Channel.Value, n.Subject, n.Body,
                n.WebhookUrl, n.Status.Value, n.RetryCount,
                n.NextRetryAt, n.CreatedAt, n.ProcessedAt))
            .ToListAsync(cancellationToken);
    }
}