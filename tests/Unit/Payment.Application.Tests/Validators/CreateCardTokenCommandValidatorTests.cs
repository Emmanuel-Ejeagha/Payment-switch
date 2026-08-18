using Payment.Application.Features.Command.CreateCardToken;

namespace Payment.Application.Tests.Validators;

public class CreateCardTokenCommandValidatorTests
{
    private readonly CreateCardTokenCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateCardTokenCommand(Guid.NewGuid(), "4242424242424242", 12, 2030));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreateCardTokenCommand(Guid.Empty, "4242424242424242", 12, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void InvalidLuhnCardNumber_Fails()
    {
        var result = _validator.Validate(new CreateCardTokenCommand(Guid.NewGuid(), "4242424242424241", 12, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "CardNumber");
    }

    [Fact]
    public void OutOfRangeExpiryMonth_Fails()
    {
        var result = _validator.Validate(new CreateCardTokenCommand(Guid.NewGuid(), "4242424242424242", 0, 2030));

        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryMonth");
    }

    [Fact]
    public void PastExpiryYear_Fails()
    {
        var result = _validator.Validate(new CreateCardTokenCommand(Guid.NewGuid(), "4242424242424242", 12, 2023));

        Assert.Contains(result.Errors, e => e.PropertyName == "ExpiryYear");
    }
}