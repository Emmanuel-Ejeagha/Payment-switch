using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.RotateWebhookSecret;

namespace Merchant.Application.Tests.Validators;

public class RotateWebhookSecretCommandValidatorTests
{
    private readonly RotateWebhookSecretCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void ValidMerchantId_Passes()
    {
        var result = _validator.Validate(new RotateWebhookSecretCommand(Guid.NewGuid(), Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new RotateWebhookSecretCommand(Guid.Empty, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}