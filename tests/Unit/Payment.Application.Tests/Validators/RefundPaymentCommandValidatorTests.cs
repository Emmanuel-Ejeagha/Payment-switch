using Payment.Application.Features.Command.RefundPayment;

namespace Payment.Application.Tests.Validators;

public class RefundPaymentCommandValidatorTests
{
    private readonly RefundPaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommandWithNullAmount_Passes()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidCommandWithAmount_Passes()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), 500));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyIntentId_Fails()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.Empty, null));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntentId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveAmount_Fails(long amount)
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), amount));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void OverMaxLengthIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new RefundPaymentCommand(Guid.NewGuid(), null, new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }
}