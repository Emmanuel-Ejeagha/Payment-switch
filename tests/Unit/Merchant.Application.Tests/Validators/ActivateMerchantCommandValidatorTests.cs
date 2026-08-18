using Merchant.Application.Features.Commands.ActivateMerchant;

namespace Merchant.Application.Tests.Validators;

public class ActivateMerchantCommandValidatorTests
{
    private readonly ActivateMerchantCommandValidator _validator = new();

    [Fact]
    public void ValidMerchantId_Passes()
    {
        var result = _validator.Validate(new ActivateMerchantCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ActivateMerchantCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}