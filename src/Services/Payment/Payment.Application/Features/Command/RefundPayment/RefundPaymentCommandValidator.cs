using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.RefundPayment;

public class RefundPaymentCommandValidator : AbstractValidator<RefundPaymentCommand>
{
    public RefundPaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).When(x => x.Amount.HasValue)
            .WithMessage("Refund amount must be greater than zero.");
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(IdempotencyKey.MaxLength)
            .Must(IdempotencyKey.IsWellFormed).WithMessage("Idempotency key contains invalid characters.");
    }
}
