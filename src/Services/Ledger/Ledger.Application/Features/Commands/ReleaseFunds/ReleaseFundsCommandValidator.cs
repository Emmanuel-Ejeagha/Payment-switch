using FluentValidation;

namespace Ledger.Application.Features.Commands.ReleaseFunds;

public class ReleaseFundsCommandValidator : AbstractValidator<ReleaseFundsCommand>
{
    public ReleaseFundsCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}
