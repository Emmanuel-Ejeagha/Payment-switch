namespace Merchant.Application.Features.Commands.OnboardMerchant;

public class OnboardMerchantCommandValidator : AbstractValidator<OnboardMerchantCommand>
{
    public OnboardMerchantCommandValidator()
    {
        RuleFor(x => x.BusinessName)
            .NotEmpty().WithMessage("Business name is required.")
            .MinimumLength(2).WithMessage("Business name must be at least 2 characters.")
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty().EmailAddress().WithMessage("A valid email is required.");
    }
}