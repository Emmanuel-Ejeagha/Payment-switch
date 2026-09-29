using Payment.Application.Features.Command.CreatePlan;

namespace Payment.Application.Tests.Validators;

public class CreatePlanCommandValidatorTests
{
    private readonly CreatePlanCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "month", 1));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CapitalizedIntervalUnit_Passes()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "Month", 1));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.Empty, "Pro", 9900, "USD", "month", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyName_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "", 9900, "USD", "month", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void OverMaxLengthName_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), new string('x', 201), 9900, "USD", "month", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ZeroAmount_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 0, "USD", "month", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void InvalidCurrency_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "US", "month", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public void UnsupportedIntervalUnit_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "fortnight", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalUnit");
    }

    [Fact]
    public void EmptyIntervalUnit_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "", 1));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalUnit");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(53)]
    public void OutOfRangeIntervalCount_Fails(int intervalCount)
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "month", intervalCount));

        Assert.Contains(result.Errors, e => e.PropertyName == "IntervalCount");
    }

    [Fact]
    public void OverMaxLengthDescription_Fails()
    {
        var result = _validator.Validate(new CreatePlanCommand(Guid.NewGuid(), "Pro", 9900, "USD", "month", 1, new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}