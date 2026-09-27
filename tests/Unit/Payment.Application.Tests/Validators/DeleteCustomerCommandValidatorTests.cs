using Payment.Application.Features.Command.DeleteCustomer;

namespace Payment.Application.Tests.Validators;

public class DeleteCustomerCommandValidatorTests
{
    private readonly DeleteCustomerCommandValidator _validator = new();

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(new DeleteCustomerCommand(Guid.NewGuid(), Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyId_Fails()
    {
        var result = _validator.Validate(new DeleteCustomerCommand(Guid.Empty, Guid.NewGuid()));

        Assert.Contains(result.Errors, e => e.PropertyName == "Id");
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new DeleteCustomerCommand(Guid.NewGuid(), Guid.Empty));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}