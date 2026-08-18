using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.RevokeMerchantApiKey;

namespace Merchant.Application.Tests.Validators;

public class RevokeMerchantApiKeyCommandValidatorTests
{
    private readonly RevokeMerchantApiKeyCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new RevokeMerchantApiKeyCommand(Guid.NewGuid(), Guid.NewGuid(), Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new RevokeMerchantApiKeyCommand(Guid.Empty, Guid.NewGuid(), Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyKeyId_Fails()
    {
        var result = _validator.Validate(new RevokeMerchantApiKeyCommand(Guid.NewGuid(), Guid.Empty, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "KeyId");
    }
}