using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class PaymentStatus : ValueObject
{
    public string Value { get; }

    private PaymentStatus(string value) => Value = value;

    public static readonly PaymentStatus Pending = new("Pending");
    public static readonly PaymentStatus Authorized = new("Authorized");
    public static readonly PaymentStatus Captured = new("Captured");
    public static readonly PaymentStatus PartiallyCaptured = new("PartiallyCaptured");
    public static readonly PaymentStatus Voided = new("Voided");
    public static readonly PaymentStatus Failed = new("Failed");
    public static readonly PaymentStatus PartiallyRefunded = new("PartiallyRefunded");
    public static readonly PaymentStatus FullyRefunded = new("FullyRefunded");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PaymentStatus status) => status.Value;
    public override string ToString() => Value;
}