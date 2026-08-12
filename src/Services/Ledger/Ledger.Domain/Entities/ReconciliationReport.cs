using BuildingBlocks.Shared.Aggregate;

namespace Ledger.Domain.Entities;

public enum ReconciliationStatus
{
    Completed,
    MismatchFound
}

/// <summary>
/// A snapshot of a ledger reconciliation run: every account's denormalized
/// balances are recomputed from its journal (the append-only audit trail) and
/// compared against what is stored. A mismatch means money can no longer be
/// explained by debits/credits — an alert-worthy financial-integrity signal.
/// </summary>
public class ReconciliationReport : AggregateRoot
{
    public DateTime RunAtUtc { get; private set; }
    public ReconciliationStatus Status { get; private set; }
    public int TotalAccounts { get; private set; }
    public int MismatchCount { get; private set; }
    private readonly List<ReconciliationLineItem> _items = new();
    public IReadOnlyList<ReconciliationLineItem> Items => _items.AsReadOnly();

    private ReconciliationReport() : base() { }

    public ReconciliationReport(Guid id, IReadOnlyCollection<ReconciliationLineItem> items, DateTime runAtUtc) : base(id)
    {
        if (items is null) throw new ArgumentNullException(nameof(items));
        RunAtUtc = runAtUtc;
        _items = items.ToList();
        TotalAccounts = _items.Count;
        MismatchCount = _items.Count(i => !i.IsMatch);
        Status = MismatchCount > 0 ? ReconciliationStatus.MismatchFound : ReconciliationStatus.Completed;
    }
}

public class ReconciliationLineItem
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public string Currency { get; private set; } = default!;
    public long ExpectedAvailable { get; private set; }
    public long ActualAvailable { get; private set; }
    public long ExpectedPending { get; private set; }
    public long ActualPending { get; private set; }
    public long ExpectedReserved { get; private set; }
    public long ActualReserved { get; private set; }
    public bool IsMatch { get; private set; }

    private ReconciliationLineItem() { }

    public ReconciliationLineItem(
        Guid merchantId,
        string currency,
        long expectedAvailable,
        long actualAvailable,
        long expectedPending,
        long actualPending,
        long expectedReserved,
        long actualReserved)
    {
        Id = Guid.NewGuid();
        MerchantId = merchantId;
        Currency = currency;
        ExpectedAvailable = expectedAvailable;
        ActualAvailable = actualAvailable;
        ExpectedPending = expectedPending;
        ActualPending = actualPending;
        ExpectedReserved = expectedReserved;
        ActualReserved = actualReserved;
        IsMatch = ExpectedAvailable == ActualAvailable
                  && ExpectedPending == ActualPending
                  && ExpectedReserved == ActualReserved;
    }
}