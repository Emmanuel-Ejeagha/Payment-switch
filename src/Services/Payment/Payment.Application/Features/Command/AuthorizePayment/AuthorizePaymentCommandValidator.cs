using FluentValidation;

namespace Payment.Application.Features.Command.AuthorizePayment;

public class AuthorizePaymentCommandValidator : AbstractValidator<AuthorizePaymentCommand>
{
    public AuthorizePaymentCommandValidator()
    {
        RuleFor(x => x.IntentId).NotEmpty();
        RuleFor(x => x.IdempotencyKey).MaximumLength(200);
    }
}
