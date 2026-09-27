using Settlement.Application.Features.Command.TriggerSettlement;

namespace Settlement.Application.Tests.Validators;

public class TriggerSettlementCommandValidatorTests
{
    private readonly TriggerSettlementCommandValidator _validator = new();

    [Fact]
    public void Today_Passes()
    {
        var result = _validator.Validate(new TriggerSettlementCommand(DateTime.UtcNow.Date));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Tomorrow_Passes()
    {
        var result = _validator.Validate(new TriggerSettlementCommand(DateTime.UtcNow.Date.AddDays(1)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void TwoDaysFromNow_Fails()
    {
        var result = _validator.Validate(new TriggerSettlementCommand(DateTime.UtcNow.Date.AddDays(2)));

        Assert.Contains(result.Errors, e => e.PropertyName == "BatchDate");
    }

    [Fact]
    public void DefaultDate_Fails()
    {
        var result = _validator.Validate(new TriggerSettlementCommand(default));

        Assert.Contains(result.Errors, e => e.PropertyName == "BatchDate");
    }
}
