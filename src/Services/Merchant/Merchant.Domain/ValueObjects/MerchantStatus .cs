using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

public class MerchantStatus : ValueObject
{
    public string Value { get; }

    private MerchantStatus(string value) => Value = value;

    public static readonly MerchantStatus Pending = new("Pending");
    public static readonly MerchantStatus Active = new("Active");
    public static readonly MerchantStatus Suspended = new("Suspended");

    public static implicit operator string(MerchantStatus status) => status.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}