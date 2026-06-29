namespace Merchant.Application.Features.Commands.UpdateMerchantConfig;

public class UpdateMerchantConfigurationCommandValidator : AbstractValidator<UpdateMerchantConfigurationCommand>
{
    public UpdateMerchantConfigurationCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.WebhookUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            .When(x => !string.IsNullOrEmpty(x.WebhookUrl))
            .WithMessage("Webhook URL must be a valid absolute URL.");
        RuleFor(x => x.PaymentMethods)
            .Must(methods => methods == null || methods.Count > 0)
            .WithMessage("Payment methods list cannot be empty if provided.");
    }
}
