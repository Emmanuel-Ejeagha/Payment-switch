namespace Ledger.Application.Features.Commands.RefundFunds;

public record RefundFundsCommand(Guid MerchantId, decimal Amount, string Currency, string CorrelationId);
