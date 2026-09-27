using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CheckoutPayment;

public class CheckoutPaymentCommandValidator : AbstractValidator<CheckoutPaymentCommand>
{
    public CheckoutPaymentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.CardToken).NotEmpty().Matches("^card_[a-z0-9]+$")
            .WithMessage("Card token must be in the form card_<hex>.");
        RuleFor(x => x.IdempotencyKey).NotEmpty();
        RuleFor(x => x.SecurityCode)
            .Must(CardSecurityCode.IsValid)
            .When(x => x.SecurityCode is not null)
            .WithMessage("Security code must be 3 or 4 digits.");
    }
}
