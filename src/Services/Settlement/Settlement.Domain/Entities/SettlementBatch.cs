using BuildingBlocks.Shared.Aggregate;
using Settlement.Domain.DomainEvents;
using Settlement.Domain.ValueObjects;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Settlement.Domain.Tests")]


namespace Settlement.Domain.Entities;

public class SettlementBatch : AggregateRoot
{
    public DateTime BatchDate { get; private set; }
    public string? Currency { get; private set; }
    public SettlementStatus Status { get; internal set; } = null!;
    public long TotalAmount { get; internal set; }
    public IReadOnlyList<Payout> Payouts => _payouts.AsReadOnly();
    private readonly List<Payout> _payouts = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public uint RowVersion { get; private set; }
    private SettlementBatch() : base() { }

    public SettlementBatch(Guid id, DateTime batchDate) : base(id)
    {
        BatchDate = batchDate.Date;
        Status = SettlementStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        AddDomainEvent(new SettlementBatchCreatedEvent(Id, BatchDate, 0));
    }

    public void AddPayout(Guid merchantId, Money grossVolume, Money fees)
    {
        if (Status != SettlementStatus.Pending)
            throw new InvalidOperationException("Can only add payouts to a pending batch.");

        if (_payouts.Any(p => p.MerchantId == merchantId))
            throw new InvalidOperationException($"Merchant {merchantId} already exists in this batch.");

        if (grossVolume.Currency != fees.Currency)
            throw new ArgumentException("Currency mismatch between gross volume and fees.");

        // A batch holds a single currency so TotalAmount stays meaningful.
        // Cross-currency payouts belong in a separate batch for their currency.
        if (Currency is not null && !string.Equals(Currency, grossVolume.Currency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException($"Currency mismatch with batch currency '{Currency}'.");

        var payout = new Payout(merchantId, grossVolume, fees);
        _payouts.Add(payout);
        Currency ??= payout.Currency;
        TotalAmount += payout.NetAmount.Amount;
    }

    public void Complete()
    {
        if (Status == SettlementStatus.Completed)
            throw new InvalidOperationException("Batch is already completed.");

        Status = SettlementStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        AddDomainEvent(new SettlementBatchCompletedEvent(Id, BatchDate, TotalAmount, Currency ?? "USD"));
    }
}
