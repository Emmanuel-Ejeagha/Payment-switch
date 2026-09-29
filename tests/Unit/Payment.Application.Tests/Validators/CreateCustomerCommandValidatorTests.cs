using Payment.Application.Features.Command.CreateCustomer;

namespace Payment.Application.Tests.Validators;

public class CreateCustomerCommandValidatorTests
{
    private readonly CreateCustomerCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), "cust@example.com"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.Empty, "cust@example.com"));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }

    [Fact]
    public void EmptyEmail_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), "not-an-email"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void OverMaxLengthName_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), "cust@example.com", new string('x', 201)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void OverMaxLengthPhone_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), "cust@example.com", null, new string('x', 33)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Phone");
    }

    [Fact]
    public void OverMaxLengthDescription_Fails()
    {
        var result = _validator.Validate(new CreateCustomerCommand(Guid.NewGuid(), "cust@example.com", null, null, new string('x', 501)));

        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }
}