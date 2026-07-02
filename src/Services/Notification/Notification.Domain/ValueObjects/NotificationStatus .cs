using BuildingBlocks.Shared;

namespace Notification.Domain.ValueObjects;

public class NotificationStatus : ValueObject
{
    public string Value { get; }

    private NotificationStatus(string value) => Value = value;

    public static readonly NotificationStatus Pending = new("pending");
    public static readonly NotificationStatus Sent = new("sent");
    public static readonly NotificationStatus Failed = new("failed");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(NotificationStatus status) => status.Value;
    public override string ToString() => Value;
}
