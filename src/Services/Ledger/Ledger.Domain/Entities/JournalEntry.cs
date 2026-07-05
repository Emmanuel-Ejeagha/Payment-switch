using BuildingBlocks.Shared.Aggregate;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Entities;


public class JournalEntry : BaseEntity
{
    public EntryType Type { get; private set; }
    public Money Amount { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public CorrelationId CorrelationId { get; private set; } = default!;
    public DateTime Timestamp { get; private set; }

    private JournalEntry() : base() { }

    public JournalEntry(EntryType type, Money amount, string description, CorrelationId correlationId) : base()
    {
        Type = type;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        Timestamp = DateTime.UtcNow;
    }
}