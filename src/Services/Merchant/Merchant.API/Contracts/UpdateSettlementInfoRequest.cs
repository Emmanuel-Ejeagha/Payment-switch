namespace Merchant.API.Contracts;

public record UpdateSettlementInfoRequest(
    string BankAccountName,
    string BankAccountNumber,
    string BankName,
    string SettlementCurrency,
    string SettlementSchedule);