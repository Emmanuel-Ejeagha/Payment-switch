using Merchant.Application.Auth;

namespace Merchant.Application.Features.Commands.UpdateSettlementInfo;

public record UpdateSettlementInfoCommand(
    Guid MerchantId,
    string BankAccountName,
    string BankAccountNumber,
    string BankName,
    string SettlementCurrency,
    string SettlementSchedule,
    CallerContext Caller
);