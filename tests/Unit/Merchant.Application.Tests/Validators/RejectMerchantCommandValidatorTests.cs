using Merchant.Application.Features.Commands.RejectMerchant;

namespace Merchant.Application.Tests.Validators;

public class RejectMerchantCommandValidatorTests
{
    private readonly RejectMerchantCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new RejectMerchantCommand(Guid.NewGuid(), "Invalid documents"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new RejectMerchantCommand(Guid.Empty, "Invalid documents"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyReason_Fails()
    {
        var result = _validator.Validate(new RejectMerchantCommand(Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }

    [Fact]
    public void OverMaxLengthReason_Fails()
    {
        var reason = new string('r', 501);
        var result = _validator.Validate(new RejectMerchantCommand(Guid.NewGuid(), reason));

        Assert.Contains(result.Errors, e => e.PropertyName == "Reason");
    }
}