using FluentValidation;

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
            RuleFor(x => x.CardLastFour).NotEmpty().Length(4);
            RuleFor(x => x.CardBrand).NotEmpty();
        });
    }
}
