using FluentValidation;

namespace Notification.Application.Features.Commands.CreateNotification;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty().WithMessage("Recipient is required.");
        RuleFor(x => x.Channel).NotEmpty().Must(c => c is "email" or "sms" or "webhook")
            .WithMessage("Channel must be email, sms, or webhook.");
        RuleFor(x => x.Payload).NotEmpty().WithMessage("Payload is required.");
        RuleFor(x => x.MaxRetries).GreaterThanOrEqualTo(0).When(x => x.MaxRetries.HasValue);
        When(x => x.Channel == "email", () =>
        {
            RuleFor(x => x.Subject).NotEmpty().WithMessage("Subject is required for email.");
            RuleFor(x => x.Body).NotEmpty().WithMessage("Body is required for email.");
        });
        When(x => x.Channel == "webhook", () =>
        {
            RuleFor(x => x.WebhookUrl).NotEmpty().WithMessage("Webhook URL is required for webhook channel.");
        });
    }
}