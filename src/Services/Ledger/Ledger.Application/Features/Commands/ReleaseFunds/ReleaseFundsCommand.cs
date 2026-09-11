namespace Ledger.Application.Features.Commands.ReleaseFunds;

public record ReleaseFundsCommand(Guid MerchantId, long Amount, string Currency, string CorrelationId);
