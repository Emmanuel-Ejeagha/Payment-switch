using Notification.Application.Features.Commands.CreateNotification;

namespace Notification.Application.Tests.Validators;

public class CreateNotificationCommandValidatorTests
{
    private readonly CreateNotificationCommandValidator _validator = new();

    [Fact]
    public void EmailChannelWithSubjectAndBody_Passes()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "Hi", null, "{}"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SmsChannel_Passes()
    {
        var result = _validator.Validate(new CreateNotificationCommand("0712345678", "sms", null, null, null, "{}"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void WebhookChannelWithUrl_Passes()
    {
        var result = _validator.Validate(new CreateNotificationCommand("https://example.com/hook", "webhook", null, null, "https://example.com/hook", "{}"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyRecipient_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("", "email", "Hello", "Hi", null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Recipient");
    }

    [Fact]
    public void EmptyChannel_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "", "Hello", "Hi", null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Channel");
    }

    [Fact]
    public void UnsupportedChannel_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "push", "Hello", "Hi", null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Channel");
    }

    [Fact]
    public void EmptyPayload_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "Hi", null, ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Payload");
    }

    [Fact]
    public void EmailChannelWithEmptySubject_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "", "Hi", null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Subject");
    }

    [Fact]
    public void EmailChannelWithEmptyBody_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "", null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Body");
    }

    [Fact]
    public void WebhookChannelWithNullUrl_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("https://example.com/hook", "webhook", null, null, null, "{}"));

        Assert.Contains(result.Errors, e => e.PropertyName == "WebhookUrl");
    }

    [Fact]
    public void NegativeMaxRetries_Fails()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "Hi", null, "{}", -1));

        Assert.Contains(result.Errors, e => e.PropertyName == "MaxRetries");
    }

    [Fact]
    public void ZeroMaxRetries_Passes()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "Hi", null, "{}", 0));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NullMaxRetries_Passes()
    {
        var result = _validator.Validate(new CreateNotificationCommand("merchant@example.com", "email", "Hello", "Hi", null, "{}", null));

        Assert.True(result.IsValid);
    }
}
