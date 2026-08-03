using BuildingBlocks.Shared.Aggregate;
using Ledger.Domain.Enums;
using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Entities;


public class JournalEntry : BaseEntity
{
    /// <summary>Merchant-facing side: Credit when funds flow in, Debit when they flow out.</summary>
    public EntryType Type { get; private set; }
    public GlAccountCode DebitAccount { get; private set; }
    public GlAccountCode CreditAccount { get; private set; }
    public Money Amount { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public CorrelationId CorrelationId { get; private set; } = default!;
    public DateTime Timestamp { get; private set; }

    private JournalEntry() : base() { }

    public JournalEntry(EntryType type, GlAccountCode debitAccount, GlAccountCode creditAccount, Money amount, string description, CorrelationId correlationId) : base()
    {
        if (debitAccount == creditAccount)
            throw new ArgumentException("Debit and credit accounts must differ.", nameof(creditAccount));

        Type = type;
        DebitAccount = debitAccount;
        CreditAccount = creditAccount;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        Timestamp = DateTime.UtcNow;
    }
}