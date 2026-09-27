namespace Merchant.Application.Features.Commands.RejectMerchant;

public record RejectMerchantCommand(Guid MerchantId, string Reason);