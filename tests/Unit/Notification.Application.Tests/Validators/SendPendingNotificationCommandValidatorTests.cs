using Notification.Application.Features.Commands.SendPendingNotification;

namespace Notification.Application.Tests.Validators;

public class SendPendingNotificationCommandValidatorTests
{
    private readonly SendPendingNotificationCommandValidator _validator = new();

    [Fact]
    public void NonEmptyNotificationId_Passes()
    {
        var result = _validator.Validate(new SendPendingNotificationCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyNotificationId_Fails()
    {
        var result = _validator.Validate(new SendPendingNotificationCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "NotificationId");
    }
}
