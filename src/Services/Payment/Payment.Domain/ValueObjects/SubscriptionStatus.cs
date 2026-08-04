using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class SubscriptionStatus : ValueObject
{
    public string Value { get; }

    private SubscriptionStatus(string value) => Value = value;

    /// <summary>Created but never successfully charged.</summary>
    public static readonly SubscriptionStatus Incomplete = new("Incomplete");
    public static readonly SubscriptionStatus Active = new("Active");
    /// <summary>A cycle failed to collect; retries are still in flight.</summary>
    public static readonly SubscriptionStatus PastDue = new("PastDue");
    public static readonly SubscriptionStatus Canceled = new("Canceled");

    public static SubscriptionStatus FromString(string value) => value switch
    {
        "Incomplete" => Incomplete,
        "Active" => Active,
        "PastDue" => PastDue,
        "Canceled" => Canceled,
        _ => throw new ArgumentException($"Invalid subscription status: {value}", nameof(value))
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(SubscriptionStatus status) => status.Value;
    public override string ToString() => Value;
}
