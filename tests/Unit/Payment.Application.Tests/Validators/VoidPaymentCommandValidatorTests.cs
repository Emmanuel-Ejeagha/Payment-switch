using Payment.Application.Features.Command.VoidPayment;

namespace Payment.Application.Tests.Validators;

public class VoidPaymentCommandValidatorTests
{
    private readonly VoidPaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new VoidPaymentCommand(Guid.NewGuid(), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new VoidPaymentCommand(Guid.Empty, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new VoidPaymentCommand(Guid.NewGuid(), new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}