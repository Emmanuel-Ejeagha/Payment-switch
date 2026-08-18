using Merchant.Application.Auth;
using Merchant.Application.Features.Commands.UpdateContactDetails;

namespace Merchant.Application.Tests.Validators;

public class UpdateContactDetailsCommandValidatorTests
{
    private readonly UpdateContactDetailsCommandValidator _validator = new();
    private static readonly CallerContext Caller = new(Guid.NewGuid(), "owner@example.com", false, true);

    [Fact]
    public void NullOptionalFields_Passes()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.NewGuid(), null, null, null, Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidOptionalFields_Passes()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.NewGuid(), "0712345678", "12 Main St", "John Doe", Caller));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void OverMaxLengthPhone_Fails()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.NewGuid(), new string('1', 31), null, null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Phone");
    }

    [Fact]
    public void OverMaxLengthAddress_Fails()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.NewGuid(), null, new string('a', 501), null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "Address");
    }

    [Fact]
    public void OverMaxLengthContactPerson_Fails()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.NewGuid(), null, null, new string('c', 201), Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "ContactPerson");
    }

    [Fact]
    public void EmptyMerchantId_Fails()
    {
        var result = _validator.Validate(new UpdateContactDetailsCommand(Guid.Empty, null, null, null, Caller));

        Assert.Contains(result.Errors, e => e.PropertyName == "MerchantId");
    }
}