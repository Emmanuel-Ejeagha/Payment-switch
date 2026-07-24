using BuildingBlocks.Shared;

namespace Settlement.Domain.ValueObjects;

public class SettlementStatus : ValueObject
{
    public string Value { get; }

    private SettlementStatus(string value) => Value = value;

    public static SettlementStatus FromString(string value)
    {
        return value?.ToLowerInvariant() switch
        {
            "pending" => Pending,
            "processing" => Processing,
            "completed" => Completed,
            _ => throw new ArgumentException($"Invalid settlement status: {value}")
        };
    }

    public static readonly SettlementStatus Pending = new("pending");
    public static readonly SettlementStatus Processing = new("processing");
    public static readonly SettlementStatus Completed = new("completed");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(SettlementStatus status) => status.Value;
    public override string ToString() => Value;
}
