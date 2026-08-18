using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.GenerateMerchantApiKey;

namespace Merchant.Application.Tests.Validators;

public class GenerateMerchantApiKeyCommandValidatorTests
{
    private readonly GenerateMerchantApiKeyCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Theory]
    [InlineData("test")]
    [InlineData("live")]
    public void ValidEnvironment_Passes(string environment)
    {
        var result = _validator.Validate(new GenerateMerchantApiKeyCommand(Guid.NewGuid(), environment, Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new GenerateMerchantApiKeyCommand(Guid.Empty, "test", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyEnvironment_Fails()
    {
        var result = _validator.Validate(new GenerateMerchantApiKeyCommand(Guid.NewGuid(), "", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Environment");
    }

    [Fact]
    public void UnsupportedEnvironment_Fails()
    {
        var result = _validator.Validate(new GenerateMerchantApiKeyCommand(Guid.NewGuid(), "prod", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Environment");
    }
}