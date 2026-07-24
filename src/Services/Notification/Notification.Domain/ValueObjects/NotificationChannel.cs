using BuildingBlocks.Shared;

namespace Notification.Domain.ValueObjects;

public class NotificationChannel : ValueObject
{
    public string Value { get; }

    private NotificationChannel(string value) => Value = value;

    public static NotificationChannel FromString(string channel)
    {
        return channel?.ToLowerInvariant() switch
        {
            "email" => Email,
            "sms" => Sms,
            "webhook" => Webhook,
            _ => throw new ArgumentException($"Invalid notification channel: {channel}")
        };
    }

    public static readonly NotificationChannel Email = new("email");
    public static readonly NotificationChannel Sms = new("sms");
    public static readonly NotificationChannel Webhook = new("webhook");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(NotificationChannel channel) => channel.Value;
    public override string ToString() => Value;
}