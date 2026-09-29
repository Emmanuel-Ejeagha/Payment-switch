using BuildingBlocks.Shared;

namespace Payment.Domain.ValueObjects;

public class InvoiceStatus : ValueObject
{
    public string Value { get; }

    private InvoiceStatus(string value) => Value = value;

    /// <summary>Issued and awaiting collection.</summary>
    public static readonly InvoiceStatus Open = new("Open");
    public static readonly InvoiceStatus Paid = new("Paid");
    /// <summary>Collection was abandoned after exhausting retries.</summary>
    public static readonly InvoiceStatus Uncollectible = new("Uncollectible");
    public static readonly InvoiceStatus Void = new("Void");

    public static InvoiceStatus FromString(string value) => value switch
    {
        "Open" => Open,
        "Paid" => Paid,
        "Uncollectible" => Uncollectible,
        "Void" => Void,
        _ => throw new ArgumentException($"Invalid invoice status: {value}", nameof(value))
    };

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(InvoiceStatus status) => status.Value;
    public override string ToString() => Value;
}
