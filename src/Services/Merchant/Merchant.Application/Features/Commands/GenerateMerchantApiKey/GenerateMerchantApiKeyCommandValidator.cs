namespace Merchant.Application.Features.Commands.GenerateMerchantApiKey;

public class GenerateMerchantApiKeyCommandValidator : AbstractValidator<GenerateMerchantApiKeyCommand>
{
    public GenerateMerchantApiKeyCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("Merchant id is required.");

        RuleFor(x => x.Environment)
            .NotEmpty().WithMessage("Environment is required.")
            .Must(e => e == "live" || e == "test")
            .WithMessage("Environment must be 'live' or 'test'.");
    }
}
