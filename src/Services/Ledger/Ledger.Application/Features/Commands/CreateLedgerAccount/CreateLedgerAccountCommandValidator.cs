using FluentValidation;

namespace Ledger.Application.Features.Commands.CreateLedgerAccount;

public class CreateLedgerAccountCommandValidator : AbstractValidator<CreateLedgerAccountCommand>
{
    public CreateLedgerAccountCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3).WithMessage("Currency must be a 3-letter ISO code.");
    }
}