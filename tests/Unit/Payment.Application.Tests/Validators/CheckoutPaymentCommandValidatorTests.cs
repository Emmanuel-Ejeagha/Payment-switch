using Payment.Application.Features.Command.CheckoutPayment;

namespace Payment.Application.Tests.Validators;

public class CheckoutPaymentCommandValidatorTests
{
    private readonly CheckoutPaymentCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("LINK123", "card_abc123", "idem-1", null));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyCode_Fails()
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("", "card_abc123", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void InvalidCardToken_Fails()
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("LINK123", "token", "idem-1"));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardToken");
    }

    [Fact]
    public void EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("LINK123", "card_abc123", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "IdempotencyKey");
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12a")]
    public void InvalidSecurityCode_Fails(string securityCode)
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("LINK123", "card_abc123", "idem-1", securityCode));

        Assert.Contains(result.Errors, e => e.PropertyName == "SecurityCode");
    }

    [Fact]
    public void FourDigitSecurityCode_Passes()
    {
        var result = _validator.Validate(new CheckoutPaymentCommand("LINK123", "card_abc123", "idem-1", "1234"));

        Assert.True(result.IsValid);
    }
}