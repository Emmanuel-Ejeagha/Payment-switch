using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.AuthorizePayment;

public class AuthorizePaymentCommandValidator : AbstractValidator<AuthorizePaymentCommand>
{
    public AuthorizePaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty().WithMessage("Idempotency key is required.")
            .MaximumLength(IdempotencyKey.MaxLength)
            .Must(IdempotencyKey.IsWellFormed).WithMessage("Idempotency key contains invalid characters.");
    }
}
