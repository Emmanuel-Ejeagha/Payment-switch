using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.OnboardMerchant;

namespace Merchant.Application.Tests.Validators;

public class OnboardMerchantCommandValidatorTests
{
    private readonly OnboardMerchantCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new OnboardMerchantCommand("Acme Coffee", "test@example.com", Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyBusinessName_Fails()
    {
        var result = _validator.Validate(new OnboardMerchantCommand("", "test@example.com", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BusinessName");
    }

    [Fact]
    public void SingleCharacterBusinessName_Fails()
    {
        var result = _validator.Validate(new OnboardMerchantCommand("A", "test@example.com", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BusinessName");
    }

    [Fact]
    public void OverMaxLengthBusinessName_Fails()
    {
        var businessName = new string('a', 101);
        var result = _validator.Validate(new OnboardMerchantCommand(businessName, "test@example.com", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BusinessName");
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new OnboardMerchantCommand("Acme Coffee", "", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new OnboardMerchantCommand("Acme Coffee", "not-an-email", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }
}