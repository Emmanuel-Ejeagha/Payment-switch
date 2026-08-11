using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence.Repositories;

public class NotificationPreferenceRepository : INotificationPreferenceRepository
{
    private readonly AppDbContext _context;

    public NotificationPreferenceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationPreference?> FindAsync(
        string recipient, string channel, string eventType, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .FirstOrDefaultAsync(p =>
                p.Recipient == recipient &&
                p.Channel.Value == channel &&
                p.EventType == eventType, cancellationToken);
    }

    public async Task<List<NotificationPreference>> ListByRecipientAsync(
        string recipient, CancellationToken cancellationToken = default)
    {
        return await _context.NotificationPreferences
            .Where(p => p.Recipient == recipient)
            .OrderBy(p => p.EventType)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(NotificationPreference preference, CancellationToken cancellationToken = default)
    {
        await _context.NotificationPreferences.AddAsync(preference, cancellationToken);
    }
}
