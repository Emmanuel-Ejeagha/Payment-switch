namespace Ledger.Application.Features.Commands.CaptureFunds;

public record CaptureFundsCommand(Guid MerchantId, decimal Amount, string Currency, string CorrelationId);
