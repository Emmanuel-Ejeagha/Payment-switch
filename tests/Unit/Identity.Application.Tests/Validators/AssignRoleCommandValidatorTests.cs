using Identity.Application.Commands.Role;

namespace Identity.Application.Tests.Validators;

public class AssignRoleCommandValidatorTests
{
    private readonly AssignRoleCommandValidator _validator = new();

    [Fact]
    public void ValidRoleAdmin_Passes()
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Admin"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Merchant")]
    [InlineData("Support")]
    public void AllowedRole_Passes(string role)
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), role));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyAdminUserId_Fails()
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.Empty, Guid.NewGuid(), "Admin"));

        Assert.Contains(result.Errors, e => e.PropertyName == "AdminUserId");
    }

    [Fact]
    public void EmptyTargetUserId_Fails()
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.NewGuid(), Guid.Empty, "Admin"));

        Assert.Contains(result.Errors, e => e.PropertyName == "TargetUserId");
    }

    [Fact]
    public void DisallowedRole_Fails()
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Owner"));

        Assert.Contains(result.Errors, e => e.PropertyName == "Role");
    }

    [Fact]
    public void EmptyRole_Fails()
    {
        var result = _validator.Validate(new AssignRoleCommand(Guid.NewGuid(), Guid.NewGuid(), ""));

        Assert.Contains(result.Errors, e => e.PropertyName == "Role");
    }
}