namespace Ledger.Application.Features.Commands.RefundFunds;

public record RefundFundsCommand(Guid MerchantId, long Amount, string Currency, string CorrelationId);
