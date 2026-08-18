using Merchant.Application.Features.Commands.SuspendMerchant;

namespace Merchant.Application.Tests.Validators;

public class SuspendMerchantCommandValidatorTests
{
    private readonly SuspendMerchantCommandValidator _validator = new();

    [Fact]
    public void ValidMerchantId_Passes()
    {
        var result = _validator.Validate(new SuspendMerchantCommand(Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new SuspendMerchantCommand(Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}