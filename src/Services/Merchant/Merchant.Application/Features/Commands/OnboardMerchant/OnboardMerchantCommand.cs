using Merchant.Application.Auth;

namespace Merchant.Application.Features.Commands.OnboardMerchant;

public record OnboardMerchantCommand(string BusinessName, string Email, CallerContext Caller);
