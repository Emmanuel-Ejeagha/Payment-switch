using Payment.Application.Features.Command.ConfirmPaymentIntent;

namespace Payment.Application.Tests.Validators;

public class ConfirmPaymentIntentCommandValidatorTests
{
    private readonly ConfirmPaymentIntentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.Empty, Guid.NewGuid(), null));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.Empty, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}