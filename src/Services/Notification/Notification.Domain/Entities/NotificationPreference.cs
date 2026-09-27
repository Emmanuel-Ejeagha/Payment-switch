using BuildingBlocks.Shared.Aggregate;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Entities;

/// <summary>
/// Opt-in/opt-out control for a single (recipient, channel, event-type) pair.
/// Absence of a row means the event is allowed; an existing row with
/// <see cref="Enabled"/> = false suppresses delivery.
/// </summary>
public class NotificationPreference : AggregateRoot
{
    public string Recipient { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; } = default!;
    public string EventType { get; private set; } = default!;
    public bool Enabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private NotificationPreference() : base() { }

    public NotificationPreference(
        Guid id,
        string recipient,
        NotificationChannel channel,
        string eventType,
        bool enabled) : base(id)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient cannot be empty.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentException("EventType cannot be empty.", nameof(eventType));

        Recipient = recipient.Trim().ToLowerInvariant();
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        EventType = eventType;
        Enabled = enabled;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Update(bool enabled)
    {
        Enabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
