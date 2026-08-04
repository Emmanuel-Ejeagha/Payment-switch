using FluentValidation;

namespace Payment.Application.Features.Command.CreateCardToken;

public class CreateCardTokenCommandValidator : AbstractValidator<CreateCardTokenCommand>
{
    public CreateCardTokenCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.CardNumber).NotEmpty().Must(Domain.Services.CardValidation.IsValidLuhn)
            .WithMessage("Card number failed Luhn validation.");
        RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.ExpiryYear).GreaterThanOrEqualTo(2024);
    }
}
