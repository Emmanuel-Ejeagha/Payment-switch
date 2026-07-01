using FluentValidation;

namespace Ledger.Application.Features.Commands.CaptureFunds;

public class CaptureFundsCommandValidator : AbstractValidator<CaptureFundsCommand>
{
    public CaptureFundsCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.CorrelationId).NotEmpty();
    }
}