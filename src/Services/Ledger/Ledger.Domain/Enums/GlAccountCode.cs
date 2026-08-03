namespace Ledger.Domain.Enums;

/// <summary>
/// Chart of accounts for the double-entry general ledger.
/// Every journal entry records a balanced debit and credit leg against these accounts.
/// </summary>
public enum GlAccountCode
{
    /// <summary>Funds received from (or owed back to) the card network; the counterpart asset.</summary>
    Cash,

    /// <summary>Funds held at authorization and not yet captured (the true reservation).</summary>
    Reserve,

    /// <summary>Funds available to the merchant for payout.</summary>
    MerchantLiability,

    /// <summary>Revenue from processing fees.</summary>
    FeesIncome,

    /// <summary>Funds queued for settlement payout (booked by settlement execution).</summary>
    SettlementPayable
}
