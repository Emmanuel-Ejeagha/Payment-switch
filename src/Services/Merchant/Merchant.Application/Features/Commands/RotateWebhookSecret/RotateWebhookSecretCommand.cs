using Merchant.Application.Auth;

namespace Merchant.Application.Features.Commands.RotateWebhookSecret;

public record RotateWebhookSecretCommand(Guid MerchantId, CallerContext Caller);
