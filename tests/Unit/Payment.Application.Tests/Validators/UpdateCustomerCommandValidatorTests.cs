using Payment.Application.Features.Command.UpdateCustomer;

namespace Payment.Application.Tests.Validators;

public class UpdateCustomerCommandValidatorTests
{
    private readonly UpdateCustomerCommandValidator _validator = new();

    [Fact]
    public void ValidCommandWithNullEmail_Passes()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidCommandWithEmail_Passes()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), "cust@example.com"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyId_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.Empty, Guid.NewGuid()));

        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), "bad-email"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void OverMaxLengthName_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), null, new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void OverMaxLengthPhone_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, new string('x', 33)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Phone");
    }

    [Fact]
    public void OverMaxLengthDescription_Fails()
    {
        var result = _validator.Validate(new UpdateCustomerCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, null, new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}