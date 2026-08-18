using Payment.Application.Features.Command.CreatePaymentLink;

namespace Payment.Application.Tests.Validators;

public class CreatePaymentLinkCommandValidatorTests
{
    private readonly CreatePaymentLinkCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreatePaymentLinkCommand(Guid.NewGuid(), 10000, "USD", "Summer sale"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreatePaymentLinkCommand(Guid.Empty, 10000, "USD", "Summer sale"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        var result = _validator.Validate(new CreatePaymentLinkCommand(Guid.NewGuid(), 0, "USD", "Summer sale"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var result = _validator.Validate(new CreatePaymentLinkCommand(Guid.NewGuid(), 10000, "US", "Summer sale"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void OverMaxLengthDescription_Fails()
    {
        var result = _validator.Validate(new CreatePaymentLinkCommand(Guid.NewGuid(), 10000, "USD", new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}