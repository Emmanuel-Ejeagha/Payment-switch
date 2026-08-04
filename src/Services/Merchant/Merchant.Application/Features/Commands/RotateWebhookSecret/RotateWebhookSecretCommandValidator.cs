using FluentValidation;

namespace Merchant.Application.Features.Commands.RotateWebhookSecret;

public class RotateWebhookSecretCommandValidator : AbstractValidator<RotateWebhookSecretCommand>
{
    public RotateWebhookSecretCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
    }
}
