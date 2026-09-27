using Notification.Domain.Entities;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Tests;

public class NotificationPreferenceTests
{
    [Fact]
    public void Create_Valid_StoresNormalizedRecipient()
    {
        var preference = new NotificationPreference(
            Guid.NewGuid(), "  Merchant@Acme.COM ", NotificationChannel.Email,
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, false);

        Assert.Equal("merchant@acme.com", preference.Recipient);
        Assert.Equal("email", preference.Channel.Value);
        Assert.Equal(Notification.Domain.NotificationEventTypes.PaymentAuthorized, preference.EventType);
        Assert.False(preference.Enabled);
    }

    [Fact]
    public void Create_EmptyRecipient_Throws()
    {
        Assert.Throws<ArgumentException>(() => new NotificationPreference(
            Guid.NewGuid(), " ", NotificationChannel.Email, Notification.Domain.NotificationEventTypes.PaymentAuthorized, true));
    }

    [Fact]
    public void Create_EmptyEventType_Throws()
    {
        Assert.Throws<ArgumentException>(() => new NotificationPreference(
            Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email, "", true));
    }

    [Fact]
    public void Update_TogglesEnabled()
    {
        var preference = new NotificationPreference(
            Guid.NewGuid(), "merchant@acme.com", NotificationChannel.Email,
            Notification.Domain.NotificationEventTypes.PaymentAuthorized, true);

        preference.Update(false);

        Assert.False(preference.Enabled);

        preference.Update(true);

        Assert.True(preference.Enabled);
    }
}
