using BuildingBlocks.Shared.Aggregate;
using Ledger.Domain.DomainEvents;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ledger.Domain.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Ledger.Application.Tests")]

namespace Ledger.Domain.Entities;

public class LedgerAccount : AggregateRoot
{
    public Guid MerchantId { get; private set; }
    public long AvailableBalance { get; internal set; }
    public long PendingBalance { get; internal set; }
    public long ReservedBalance { get; internal set; }
    public string Currency { get; private set; } = default!;
    public IReadOnlyList<JournalEntry> Journal => _journal;
    private List<JournalEntry> _journal = new();
    public uint RowVersion { get; private set; }
    private LedgerAccount() : base() { }

    public LedgerAccount(Guid id, Guid merchantId, string currency) : base(id)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentNullException(nameof(currency));
        MerchantId = merchantId;
        Currency = currency.ToUpperInvariant();
        AvailableBalance = 0;
        PendingBalance = 0;
        ReservedBalance = 0;
    }

    public void ReserveFunds(Money amount, CorrelationId correlationId)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");

        // True reservation: the authorized amount is held (Reserve) until capture.
        PendingBalance += amount.Amount;
        ReservedBalance += amount.Amount;

        var entry = new JournalEntry(EntryType.Credit, GlAccountCode.Cash, GlAccountCode.Reserve, amount, "Funds reserved", correlationId);
        _journal.Add(entry);
        AddDomainEvent(new FundsReservedEvent(MerchantId, amount, correlationId.Value));
    }

    public void CaptureFunds(Money amount, CorrelationId correlationId)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");
        if (PendingBalance < amount.Amount)
            throw new InvalidOperationException("Insufficient pending funds.");

        PendingBalance -= amount.Amount;
        ReservedBalance -= amount.Amount;
        AvailableBalance += amount.Amount;

        var entry = new JournalEntry(EntryType.Credit, GlAccountCode.Reserve, GlAccountCode.MerchantLiability, amount, "Funds captured", correlationId);
        _journal.Add(entry);
        AddDomainEvent(new FundsCapturedEvent(MerchantId, amount, correlationId.Value));
    }

    public void RefundFunds(Money amount, CorrelationId correlationId)
    {
        if (amount.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");
        if (AvailableBalance < amount.Amount)
            throw new InvalidOperationException("Insufficient available funds.");

        AvailableBalance -= amount.Amount;

        var entry = new JournalEntry(EntryType.Debit, GlAccountCode.MerchantLiability, GlAccountCode.Cash, amount, "Funds refunded", correlationId);
        _journal.Add(entry);
        AddDomainEvent(new FundsRefundedEvent(MerchantId, amount, correlationId.Value));
    }

    public void ChargeFees(Money fees, CorrelationId correlationId)
    {
        if (fees.Currency != Currency)
            throw new InvalidOperationException("Currency mismatch.");
        if (fees.Amount <= 0)
            return;

        if (AvailableBalance < fees.Amount)
            throw new InvalidOperationException("Insufficient available funds to charge fees.");

        AvailableBalance -= fees.Amount;

        var entry = new JournalEntry(EntryType.Debit, GlAccountCode.MerchantLiability, GlAccountCode.FeesIncome, fees, "Processing fees", correlationId);
        _journal.Add(entry);
        AddDomainEvent(new FeesChargedEvent(MerchantId, fees, correlationId.Value));
    }
}