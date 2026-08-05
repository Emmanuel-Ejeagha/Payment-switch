using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CreatePaymentIntent;

public class CreatePaymentIntentCommandValidator : AbstractValidator<CreatePaymentIntentCommand>
{
    public CreatePaymentIntentCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code.");
        RuleFor(x => x.PaymentMethod).NotEmpty().Must(m => m is "Card" or "Bank" or "MobileMoney")
            .WithMessage("Payment method must be Card, Bank, or MobileMoney.");
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage("Idempotency key is required.");
        When(x => x.PaymentMethod == "Card", () =>
        {
            When(x => x.CardToken is null, () =>
            {
                RuleFor(x => x.CardLastFour).NotEmpty().Length(4);
                RuleFor(x => x.CardBrand).NotEmpty();
            });
            When(x => x.CardToken is not null, () =>
            {
                RuleFor(x => x.CardToken).Matches("^card_[a-z0-9]+$")
                    .WithMessage("Card token must be in the form card_<hex>.");
            });
        });
        // Optional: absent for merchant-initiated and recurring charges. When supplied
        // it must be well-formed, so a typo fails here instead of at the acquirer.
        RuleFor(x => x.SecurityCode)
            .Must(CardSecurityCode.IsValid)
            .When(x => x.SecurityCode is not null)
            .WithMessage("Security code must be 3 or 4 digits.");
    }
}
