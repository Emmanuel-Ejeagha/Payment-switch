using FluentValidation;
using Payment.Domain.ValueObjects;

namespace Payment.Application.Features.Command.CreatePlan;

public class CreatePlanCommandValidator : AbstractValidator<CreatePlanCommand>
{
    public CreatePlanCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("MerchantId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Plan name is required.")
            .MaximumLength(200).WithMessage("Plan name must not exceed 200 characters.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.");

        RuleFor(x => x.IntervalUnit)
            .NotEmpty().WithMessage("Interval unit is required.")
            .Must(u => BillingInterval.AllowedUnits.Contains(u.Trim().ToLowerInvariant()))
            .WithMessage($"Interval unit must be one of: {string.Join(", ", BillingInterval.AllowedUnits)}.");

        RuleFor(x => x.IntervalCount)
            .GreaterThan(0).WithMessage("Interval count must be at least 1.")
            .LessThanOrEqualTo(52).WithMessage("Interval count must not exceed 52.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
