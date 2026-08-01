namespace Merchant.Application.Features.Commands.OnboardMerchant;

public record OnboardMerchantCommand(Guid OwnerId, string BusinessName, string Email);
