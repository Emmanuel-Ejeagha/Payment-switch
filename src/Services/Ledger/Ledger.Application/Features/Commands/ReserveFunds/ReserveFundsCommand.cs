namespace Ledger.Application.Features.Commands.ReserveFunds;

public record ReserveFundsCommand(Guid MerchantId, long Amount, string Currency, string CorrelationId);
