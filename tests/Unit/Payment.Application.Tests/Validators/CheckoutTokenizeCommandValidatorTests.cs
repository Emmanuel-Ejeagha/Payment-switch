using Payment.Application.Features.Command.CheckoutTokenize;

namespace Payment.Application.Tests.Validators;

public class CheckoutTokenizeCommandValidatorTests
{
    private readonly CheckoutTokenizeCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CheckoutTokenizeCommand("LINK123", "4242424242424242", 12, 2030));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyCode_Fails()
    {
        var result = _validator.Validate(new CheckoutTokenizeCommand("", "4242424242424242", 12, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "Code");
    }

    [Fact]
    public void InvalidLuhnCardNumber_Fails()
    {
        var result = _validator.Validate(new CheckoutTokenizeCommand("LINK123", "4242424242424241", 12, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardNumber");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void OutOfRangeExpiryMonth_Fails(int month)
    {
        var result = _validator.Validate(new CheckoutTokenizeCommand("LINK123", "4242424242424242", month, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryMonth");
    }

    [Fact]
    public void PastExpiryYear_Fails()
    {
        var result = _validator.Validate(new CheckoutTokenizeCommand("LINK123", "4242424242424242", 12, 2023));

        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryYear");
    }
}