using FluentValidation;

namespace Payment.Application.Features.Command.SendTestWebhookEvent;

public class SendTestWebhookEventCommandValidator : AbstractValidator<SendTestWebhookEventCommand>
{
    public SendTestWebhookEventCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.EventType).NotEmpty().MaximumLength(100);
    }
}
