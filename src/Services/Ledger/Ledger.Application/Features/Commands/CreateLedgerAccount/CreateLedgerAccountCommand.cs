namespace Ledger.Application.Features.Commands.CreateLedgerAccount;

public record CreateLedgerAccountCommand(Guid MerchantId, string Currency);
