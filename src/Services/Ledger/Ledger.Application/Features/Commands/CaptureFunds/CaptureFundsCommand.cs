namespace Ledger.Application.Features.Commands.CaptureFunds;

public record CaptureFundsCommand(Guid MerchantId, long Amount, string Currency, string CorrelationId);
