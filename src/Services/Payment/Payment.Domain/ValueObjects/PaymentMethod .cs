using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class PaymentMethod : ValueObject
{
    public string Value { get; }

    private PaymentMethod(string value) => Value = value;

    public static readonly PaymentMethod Card = new("Card");
    public static readonly PaymentMethod Bank = new("Bank");
    public static readonly PaymentMethod MobileMoney = new("MobileMoney");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PaymentMethod method) => method.Value;
    public override string ToString() => Value;
}