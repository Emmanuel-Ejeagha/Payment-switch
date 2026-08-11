using Notification.Application.Services;
using Notification.Domain.Entities;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Tests.Services;

public class NotificationPreferenceRulesTests
{
    [Fact]
    public void IsSuppressed_NoPreference_IsNotSuppressed()
    {
        Assert.False(NotificationPreferenceRules.IsSuppressed(null));
    }

    [Fact]
    public void IsSuppressed_EnabledPreference_IsNotSuppressed()
    {
        var preference = new NotificationPreference(
            Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, true);

        Assert.False(NotificationPreferenceRules.IsSuppressed(preference));
    }

    [Fact]
    public void IsSuppressed_DisabledPreference_IsSuppressed()
    {
        var preference = new NotificationPreference(
            Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, false);

        Assert.True(NotificationPreferenceRules.IsSuppressed(preference));
    }
}
