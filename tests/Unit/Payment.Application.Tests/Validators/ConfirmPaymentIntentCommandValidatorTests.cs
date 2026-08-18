using Payment.Application.Features.Command.ConfirmPaymentIntent;

namespace Payment.Application.Tests.Validators;

public class ConfirmPaymentIntentCommandValidatorTests
{
    private readonly ConfirmPaymentIntentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), "confirm-key"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.Empty, Guid.NewGuid(), "confirm-key"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.Empty, "confirm-key"));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void InvalidIdempotencyKeyCharacters_Fails()
    {
        var result = _validator.Validate(new ConfirmPaymentIntentCommand(Guid.NewGuid(), Guid.NewGuid(), "key with spaces!"));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}