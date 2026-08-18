using Ledger.Application.Features.Commands.ReserveFunds;

namespace Ledger.Application.Tests.Validators;

public class ReserveFundsCommandValidatorTests
{
    private readonly ReserveFundsCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new ReserveFundsCommand(Guid.NewGuid(), 10000, "USD", "abc"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new ReserveFundsCommand(Guid.Empty, 10000, "USD", "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidAmount_Fails(long amount)
    {
        var result = _validator.Validate(new ReserveFundsCommand(Guid.NewGuid(), amount, "USD", "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var result = _validator.Validate(new ReserveFundsCommand(Guid.NewGuid(), 10000, "US", "abc"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void EmptyCorrelationId_Fails()
    {
        var result = _validator.Validate(new ReserveFundsCommand(Guid.NewGuid(), 10000, "USD", ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "CorrelationId");
    }
}
