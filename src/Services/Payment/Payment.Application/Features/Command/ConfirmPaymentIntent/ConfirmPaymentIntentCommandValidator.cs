using FluentValidation;

namespace Payment.Application.Features.Command.ConfirmPaymentIntent;

public class ConfirmPaymentIntentCommandValidator : AbstractValidator<ConfirmPaymentIntentCommand>
{
    public ConfirmPaymentIntentCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(200);
    }
}
