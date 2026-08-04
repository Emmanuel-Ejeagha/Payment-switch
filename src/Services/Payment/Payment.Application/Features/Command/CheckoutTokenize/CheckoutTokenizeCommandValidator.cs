using FluentValidation;
using Payment.Domain.Services;

namespace Payment.Application.Features.Command.CheckoutTokenize;

public class CheckoutTokenizeCommandValidator : AbstractValidator<CheckoutTokenizeCommand>
{
    public CheckoutTokenizeCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.CardNumber).NotEmpty().Must(CardValidation.IsValidLuhn)
            .WithMessage("Card number failed Luhn validation.");
        RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ExpiryYear).GreaterThanOrEqualTo(2024);
    }
}
