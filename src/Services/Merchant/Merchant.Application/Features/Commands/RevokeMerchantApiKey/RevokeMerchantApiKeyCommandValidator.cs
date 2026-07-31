namespace Merchant.Application.Features.Commands.RevokeMerchantApiKey;

public class RevokeMerchantApiKeyCommandValidator : AbstractValidator<RevokeMerchantApiKeyCommand>
{
    public RevokeMerchantApiKeyCommandValidator()
    {
        RuleFor(x => x.MerchantId)
            .NotEmpty().WithMessage("Merchant id is required.");

        RuleFor(x => x.KeyId)
            .NotEmpty().WithMessage("API key id is required.");
    }
}
