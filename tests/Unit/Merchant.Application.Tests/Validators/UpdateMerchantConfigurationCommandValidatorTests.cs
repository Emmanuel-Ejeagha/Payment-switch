using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.UpdateMerchantConfig;

namespace Merchant.Application.Tests.Validators;

public class UpdateMerchantConfigurationCommandValidatorTests
{
    private readonly UpdateMerchantConfigurationCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void NullOptionalFields_Passes()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), null, null, null, Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidWebhookUrl_Passes()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), "https://example.com/hook", null, null, Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvalidWebhookUrl_Fails()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), "not-a-url", null, null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "WebhookUrl");
    }

    [Fact]
    public void UnsupportedSchemeWebhookUrl_Fails()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), "ftp://example.com", null, null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "WebhookUrl");
    }

    [Fact]
    public void EmptyPaymentMethods_Fails()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), null, new List<string>(), null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "PaymentMethods");
    }

    [Fact]
    public void NonEmptyPaymentMethods_Passes()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.NewGuid(), null, new List<string> { "card" }, null, Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new UpdateMerchantConfigurationCommand(Guid.Empty, null, null, null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}