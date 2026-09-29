using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.ConfirmPaymentIntent;

public class ConfirmPaymentIntentCommandValidator : AbstractValidator<ConfirmPaymentIntentCommand>
{
    public ConfirmPaymentIntentCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(IdempotencyKey.MaxLength)
            .WithMessage($"Idempotency key must not exceed {IdempotencyKey.MaxLength} characters.")
            .Must(IdempotencyKey.IsWellFormed)
            .WithMessage("Idempotency key may contain only letters, digits, hyphens, and underscores.");
    }
}
