namespace Ledger.Application.Features.Commands.ReserveFunds;

public record ReserveFundsCommand(Guid MerchantId, decimal Amount, string Currency, string CorrelationId);
