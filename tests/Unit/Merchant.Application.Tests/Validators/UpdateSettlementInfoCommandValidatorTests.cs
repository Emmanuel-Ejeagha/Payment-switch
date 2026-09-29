using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.UpdateSettlementInfo;

namespace Merchant.Application.Tests.Validators;

public class UpdateSettlementInfoCommandValidatorTests
{
    private readonly UpdateSettlementInfoCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void ValidDetails_Passes()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "USD", "DAILY", Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void LowercaseSchedule_Passes()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "USD", "daily", Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.Empty, "Acme Ltd", "ABC123", "Test Bank", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyBankAccountName_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "", "ABC123", "Test Bank", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BankAccountName");
    }

    [Fact]
    public void OverMaxLengthBankAccountName_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), new string('a', 201), "ABC123", "Test Bank", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BankAccountName");
    }

    [Fact]
    public void EmptyBankAccountNumber_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "", "Test Bank", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BankAccountNumber");
    }

    [Fact]
    public void OverMaxLengthBankAccountNumber_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", new string('1', 51), "Test Bank", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BankAccountNumber");
    }

    [Fact]
    public void EmptyBankName_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "", "USD", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "BankName");
    }

    [Fact]
    public void ShortCurrency_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "US", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "SettlementCurrency");
    }

    [Fact]
    public void NonLetterCurrency_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "US1", "DAILY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "SettlementCurrency");
    }

    [Fact]
    public void UnsupportedSchedule_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "USD", "YEARLY", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "SettlementSchedule");
    }

    [Fact]
    public void EmptySchedule_Fails()
    {
        var result = _validator.Validate(new UpdateSettlementInfoCommand(Guid.NewGuid(), "Acme Ltd", "ABC123", "Test Bank", "USD", "", Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "SettlementSchedule");
    }
}