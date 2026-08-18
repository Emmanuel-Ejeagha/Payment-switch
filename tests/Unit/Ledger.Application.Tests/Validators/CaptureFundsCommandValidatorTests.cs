using Ledger.Application.Features.Commands.CaptureFunds;

namespace Ledger.Application.Tests.Validators;

public class CaptureFundsCommandValidatorTests
{
    private readonly CaptureFundsCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new CaptureFundsCommand(Guid.NewGuid(), 10000, "USD", "abc"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CaptureFundsCommand(Guid.Empty, 10000, "USD", "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void InvalidAmount_Fails(long amount)
    {
        var result = _validator.Validate(new CaptureFundsCommand(Guid.NewGuid(), amount, "USD", "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    public void InvalidCurrency_Fails(string currency)
    {
        var result = _validator.Validate(new CaptureFundsCommand(Guid.NewGuid(), 10000, currency, "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void EmptyCorrelationId_Fails()
    {
        var result = _validator.Validate(new CaptureFundsCommand(Guid.NewGuid(), 10000, "USD", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "CorrelationId");
    }
}
