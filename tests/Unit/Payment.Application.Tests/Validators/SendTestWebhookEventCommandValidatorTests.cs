using Payment.Application.Features.Command.SendTestWebhookEvent;

namespace Payment.Application.Tests.Validators;

public class SendTestWebhookEventCommandValidatorTests
{
    private readonly SendTestWebhookEventCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new SendTestWebhookEventCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new SendTestWebhookEventCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyEventType_Fails()
    {
        var result = _validator.Validate(new SendTestWebhookEventCommand(Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "EventType");
    }

    [Fact]
    public void OverMaxLengthEventType_Fails()
    {
        var result = _validator.Validate(new SendTestWebhookEventCommand(Guid.NewGuid(), new string('x', 101)));

        Assert.Contains(result.Errors, e => e.PropertyName == "EventType");
    }
}