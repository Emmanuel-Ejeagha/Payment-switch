using FluentValidation;

namespace Notification.Application.Features.Commands.SendPendingNotification;

public class SendPendingNotificationCommandValidator : AbstractValidator<SendPendingNotificationCommand>
{
    public SendPendingNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("Notification ID is required.");
    }
}