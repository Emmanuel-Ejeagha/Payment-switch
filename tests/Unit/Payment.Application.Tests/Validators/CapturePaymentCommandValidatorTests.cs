using Payment.Application.Features.Command.CapturePayment;

namespace Payment.Application.Tests.Validators;

public class CapturePaymentCommandValidatorTests
{
    private readonly CapturePaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommandWithNullAmount_Passes()
    {
        var result = _validator.Validate(new CapturePaymentCommand(Guid.NewGuid(), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidCommandWithAmount_Passes()
    {
        var result = _validator.Validate(new CapturePaymentCommand(Guid.NewGuid(), 10000));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new CapturePaymentCommand(Guid.Empty, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void NonPositiveAmount_Fails(long amount)
    {
        var result = _validator.Validate(new CapturePaymentCommand(Guid.NewGuid(), amount));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new CapturePaymentCommand(Guid.NewGuid(), null, new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}