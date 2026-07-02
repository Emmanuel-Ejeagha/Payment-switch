using Notification.Domain.DomainEvents;
using Notification.Domain.ValueObjects;
using NotificationEntity = Notification.Domain.Entities.Notification;

namespace Notification.Domain.Tests;

public class NotificationTests
{
    [Fact]
    public void Constructor_ShouldInitializeAsPending()
    {
        var n = new NotificationEntity(
            Guid.NewGuid(), "user@example.com", NotificationChannel.Email,
            "Subject", "Body", null, "{}");

        Assert.Equal(NotificationStatus.Pending, n.Status);
        Assert.Equal(0, n.RetryCount);
        Assert.Equal(5, n.MaxRetries);
        Assert.Null(n.NextRetryAt);
        Assert.Equal("user@example.com", n.Recipient);
    }

    [Fact]
    public void MarkAsSent_ShouldSetStatusAndRaiseEvent()
    {
        var n = CreatePendingNotification();

        n.MarkAsSent();

        Assert.Equal(NotificationStatus.Sent, n.Status);
        Assert.NotNull(n.ProcessedAt);
        Assert.Contains(n.DomainEvents, e => e is NotificationSentEvent);
    }

    [Fact]
    public void MarkAsSent_AlreadySent_Throws()
    {
        var n = CreatePendingNotification();
        n.MarkAsSent();
        Assert.Throws<InvalidOperationException>(() => n.MarkAsSent());
    }

    [Fact]
    public void MarkAsFailed_ShouldRetry()
    {
        var n = CreatePendingNotification();

        n.MarkAsFailed();

        Assert.Equal(1, n.RetryCount);
        Assert.Equal(NotificationStatus.Pending, n.Status);
        Assert.NotNull(n.NextRetryAt);
        Assert.True(n.NextRetryAt > DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsFailed_MaxRetriesExceeded_ShouldFailPermanently()
    {
        var n = CreatePendingNotification();
        for (int i = 0; i < 5; i++)
            n.MarkAsFailed(); // 5 retries = max

        Assert.Equal(5, n.RetryCount);
        Assert.Equal(NotificationStatus.Failed, n.Status);
        Assert.Null(n.NextRetryAt);
        Assert.Contains(n.DomainEvents, e => e is NotificationPermanentlyFailedEvent);
    }

    [Fact]
    public void MarkAsFailed_OnSentNotification_Throws()
    {
        var n = CreatePendingNotification();
        n.MarkAsSent();
        Assert.Throws<InvalidOperationException>(() => n.MarkAsFailed());
    }

    [Fact]
    public void Channel_FromString_ValidAndInvalid()
    {
        Assert.Equal(NotificationChannel.Email, NotificationChannel.FromString("email"));
        Assert.Equal(NotificationChannel.Webhook, NotificationChannel.FromString("WEBHOOK"));
        Assert.Throws<ArgumentException>(() => NotificationChannel.FromString("push"));
    }

    private NotificationEntity CreatePendingNotification() =>
        new(Guid.NewGuid(), "test@test.com", NotificationChannel.Email,
            "Test", "Hello", null, "{}");
}