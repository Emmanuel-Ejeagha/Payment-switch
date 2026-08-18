using Payment.Application.Features.Command.CreateSubscription;

namespace Payment.Application.Tests.Validators;

public class CreateSubscriptionCommandValidatorTests
{
    private readonly CreateSubscriptionCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "card_abc1"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "card_abc1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyCustomerId_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "card_abc1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void EmptyPlanId_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "card_abc1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "PlanId");
    }

    [Fact]
    public void EmptyCardToken_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardToken");
    }

    [Fact]
    public void OverMaxLengthCardToken_Fails()
    {
        var result = _validator.Validate(new CreateSubscriptionCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('x', 101)));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardToken");
    }
}