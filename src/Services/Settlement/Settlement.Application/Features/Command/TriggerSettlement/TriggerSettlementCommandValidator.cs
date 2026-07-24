using FluentValidation;

namespace Settlement.Application.Features.Command.TriggerSettlement;

public class TriggerSettlementCommandValidator : AbstractValidator<TriggerSettlementCommand>
{
    public TriggerSettlementCommandValidator()
    {
        RuleFor(x => x.BatchDate)
            .NotEmpty()
            .Must(date => date <= DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("Batch date cannot be too far in the future.");
    }
}
