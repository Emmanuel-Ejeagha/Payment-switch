using FluentValidation;

namespace Notification.Application.Features.Commands.UpdateNotificationPreference;

public record UpdateNotificationPreferenceCommand(
    string Recipient,
    string Channel,
    string EventType,
    bool Enabled);

public class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    public UpdateNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.Recipient).NotEmpty().EmailAddress().WithMessage("A valid recipient email is required.");
        RuleFor(x => x.Channel).NotEmpty().Must(c => c is "email" or "sms" or "webhook")
            .WithMessage("Channel must be email, sms, or webhook.");
        RuleFor(x => x.EventType).NotEmpty()
            .Must(Notification.Domain.NotificationEventTypes.IsValid)
            .WithMessage("Event type is not supported.");
    }
}
