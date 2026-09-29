using BuildingBlocks.Shared.Auth;
using System.Security.Claims;

namespace BuildingBlocks.Shared.Tests;

public class RoleNamesTests
{
    [Theory]
    [InlineData("Admin", "Admin")]
    [InlineData("admin", "Admin")]
    [InlineData("ADMIN", "Admin")]
    [InlineData(" Admin ", "Admin")]
    [InlineData("merchant", "Merchant")]
    [InlineData("SUPPORT", "Support")]
    [InlineData(" support ", "Support")]
    public void Normalize_KnownRoles_ReturnsCanonical(string input, string expected)
    {
        Assert.Equal(expected, RoleNames.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("root")]
    [InlineData("SuperAdmin")]
    public void Normalize_UnknownRoles_ReturnsNull(string? input)
    {
        Assert.Null(RoleNames.Normalize(input));
        Assert.False(RoleNames.IsKnown(input));
    }

    [Fact]
    public void Policies_MatchDocumentedMatrix()
    {
        Assert.Equal("Admin", RolePolicies.AdminOnly);
        Assert.Equal("Admin,Support", RolePolicies.ReadOnly);
    }

    private static ClaimsPrincipal PrincipalWithRoles(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"));

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData(" Admin ")]
    public void IsInRole_MatchesCaseInsensitively(string role)
    {
        Assert.True(RoleNames.IsInRole(PrincipalWithRoles(role), RoleNames.Admin));
    }

    [Fact]
    public void IsInRole_SupportIsNotAdmin()
    {
        Assert.False(RoleNames.IsInRole(PrincipalWithRoles("Support"), RoleNames.Admin));
        Assert.True(RoleNames.IsInRole(PrincipalWithRoles("Support"), RoleNames.Support));
    }

    [Fact]
    public void IsInRole_NoRolesOrNull_ReturnsFalse()
    {
        Assert.False(RoleNames.IsInRole(PrincipalWithRoles(), RoleNames.Admin));
        Assert.False(RoleNames.IsInRole(null, RoleNames.Admin));
    }
}
