using FluentValidation;

namespace Payment.Application.Features.Command.CheckoutPayment;

public class CheckoutPaymentCommandValidator : AbstractValidator<CheckoutPaymentCommand>
{
    public CheckoutPaymentCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.CardToken).NotEmpty().Matches("^card_[a-z0-9]+$")
            .WithMessage("Card token must be in the form card_<hex>.");
        RuleFor(x => x.IdempotencyKey).NotEmpty();
    }
}
