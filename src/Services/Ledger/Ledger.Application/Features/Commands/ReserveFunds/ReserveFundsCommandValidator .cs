using FluentValidation;

namespace Ledger.Application.Features.Commands.ReserveFunds;

public class ReserveFundsCommandValidator : AbstractValidator<ReserveFundsCommand>
{
    public ReserveFundsCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}