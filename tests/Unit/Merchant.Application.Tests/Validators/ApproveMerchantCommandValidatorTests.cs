using Merchant.Application.Features.Commands.ApproveMerchant;

namespace Merchant.Application.Tests.Validators;

public class ApproveMerchantCommandValidatorTests
{
    private readonly ApproveMerchantCommandValidator _validator = new();

    [Fact]
    public void ValidMerchantId_Passes()
    {
        var result = _validator.Validate(new ApproveMerchantCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ApproveMerchantCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}