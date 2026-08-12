namespace Merchant.Application.Features.Commands.UpdateSettlementInfo;

public class UpdateSettlementInfoCommandValidator : AbstractValidator<UpdateSettlementInfoCommand>
{
    private static readonly string[] AllowedSchedules = { "DAILY", "WEEKLY", "MONTHLY" };

    public UpdateSettlementInfoCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.BankAccountName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BankAccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SettlementCurrency)
            .NotEmpty().Length(3)
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("Settlement currency must be a 3-letter ISO currency code.");
        RuleFor(x => x.SettlementSchedule)
            .NotEmpty()
            .Must(s => AllowedSchedules.Contains(s.ToUpperInvariant()))
            .WithMessage("Settlement schedule must be one of: DAILY, WEEKLY, MONTHLY.");
    }
}