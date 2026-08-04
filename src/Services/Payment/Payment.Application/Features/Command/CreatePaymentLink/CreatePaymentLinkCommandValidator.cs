using FluentValidation;

namespace Payment.Application.Features.Command.CreatePaymentLink;

public class CreatePaymentLinkCommandValidator : AbstractValidator<CreatePaymentLinkCommand>
{
    public CreatePaymentLinkCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code.");
        RuleFor(x => x.Description).MaximumLength(500);
    }
}
