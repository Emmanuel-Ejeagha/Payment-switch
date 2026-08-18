using Merchant.Application.Features.Commands.ReactivateMerchant;

namespace Merchant.Application.Tests.Validators;

public class ReactivateMerchantCommandValidatorTests
{
    private readonly ReactivateMerchantCommandValidator _validator = new();

    [Fact]
    public void ValidMerchantId_Passes()
    {
        var result = _validator.Validate(new ReactivateMerchantCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ReactivateMerchantCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}