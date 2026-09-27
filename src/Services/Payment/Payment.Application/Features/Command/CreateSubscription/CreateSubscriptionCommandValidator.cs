using FluentValidation;

namespace Payment.Application.Features.Command.CreateSubscription;

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId is required.");

        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("CustomerId is required.");

        RuleFor(x => x.PlanId)
            .NotEmpty().WithMessage("PlanId is required.");

        RuleFor(x => x.CardToken)
            .NotEmpty().WithMessage("A card token is required to bill a subscription.")
            .MaximumLength(100).WithMessage("Card token must not exceed 100 characters.");
    }
}
