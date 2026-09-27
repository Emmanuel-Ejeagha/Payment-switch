using Payment.Application.Auth;
using Payment.Application.Features.Command.ReplayWebhookEvent;

namespace Payment.Application.Tests.Validators;

public class ReplayWebhookEventCommandValidatorTests
{
    private readonly ReplayWebhookEventCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new ReplayWebhookEventCommand(Guid.NewGuid(), Guid.NewGuid(), new CallerContext(Guid.NewGuid(), "owner@example.com", false)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ReplayWebhookEventCommand(Guid.Empty, Guid.NewGuid(), new CallerContext(Guid.NewGuid(), "owner@example.com", false)));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyEventId_Fails()
    {
        var result = _validator.Validate(new ReplayWebhookEventCommand(Guid.NewGuid(), Guid.Empty, new CallerContext(Guid.NewGuid(), "owner@example.com", false)));

        Assert.Contains(result.Errors, e => e.PropertyName == "EventId");
    }
}