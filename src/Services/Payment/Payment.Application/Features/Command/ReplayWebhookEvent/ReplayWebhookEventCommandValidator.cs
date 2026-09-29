using FluentValidation;

namespace Payment.Application.Features.Command.ReplayWebhookEvent;

public class ReplayWebhookEventCommandValidator : AbstractValidator<ReplayWebhookEventCommand>
{
    public ReplayWebhookEventCommandValidator()
    {
        RuleFor(x => x.MerchantId).NotEmpty();
        RuleFor(x => x.EventId).NotEmpty();
    }
}
