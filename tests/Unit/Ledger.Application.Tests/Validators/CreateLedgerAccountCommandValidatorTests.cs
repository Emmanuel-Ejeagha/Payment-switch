using Ledger.Application.Features.Commands.CreateLedgerAccount;

namespace Ledger.Application.Tests.Validators;

public class CreateLedgerAccountCommandValidatorTests
{
    private readonly CreateLedgerAccountCommandValidator _validator = new();

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new CreateLedgerAccountCommand(Guid.NewGuid(), "USD"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreateLedgerAccountCommand(Guid.Empty, "USD"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyCurrency_Fails()
    {
        var result = _validator.Validate(new CreateLedgerAccountCommand(Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    public void InvalidCurrency_Fails(string currency)
    {
        var result = _validator.Validate(new CreateLedgerAccountCommand(Guid.NewGuid(), currency));

        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }
}
