using FluentValidation;

namespace Ledger.Application.Features.Commands.RefundFunds;

public class RefundFundsCommandValidator : AbstractValidator<RefundFundsCommand>
{
    public RefundFundsCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}