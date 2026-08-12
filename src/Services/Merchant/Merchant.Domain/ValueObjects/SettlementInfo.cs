using BuildingBlocks.Shared;

namespace Merchant.Domain.ValueObjects;

/// <summary>
/// Bank/settlement details required to actually pay a merchant. Money remains
/// integer minor-units on the ledger; the settlement <see cref="SettlementCurrency"/>
/// is an ISO-4217 code and <see cref="SettlementSchedule"/> selects the payout cadence.
/// </summary>
public class SettlementInfo : ValueObject
{
    public string BankAccountName { get; }
    public string BankAccountNumber { get; }
    public string BankName { get; }
    public string SettlementCurrency { get; }
    public string SettlementSchedule { get; }

    public SettlementInfo(
        string bankAccountName,
        string bankAccountNumber,
        string bankName,
        string settlementCurrency,
        string settlementSchedule)
    {
        if (string.IsNullOrWhiteSpace(bankAccountName)) throw new ArgumentNullException(nameof(bankAccountName));
        if (string.IsNullOrWhiteSpace(bankAccountNumber)) throw new ArgumentNullException(nameof(bankAccountNumber));
        if (string.IsNullOrWhiteSpace(bankName)) throw new ArgumentNullException(nameof(bankName));
        if (string.IsNullOrWhiteSpace(settlementCurrency)) throw new ArgumentNullException(nameof(settlementCurrency));

        BankAccountName = bankAccountName.Trim();
        BankAccountNumber = bankAccountNumber.Trim();
        BankName = bankName.Trim();
        SettlementCurrency = settlementCurrency.ToUpperInvariant();
        SettlementSchedule = string.IsNullOrWhiteSpace(settlementSchedule)
            ? "DAILY"
            : settlementSchedule.Trim().ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return BankAccountName;
        yield return BankAccountNumber;
        yield return BankName;
        yield return SettlementCurrency;
        yield return SettlementSchedule;
    }
}