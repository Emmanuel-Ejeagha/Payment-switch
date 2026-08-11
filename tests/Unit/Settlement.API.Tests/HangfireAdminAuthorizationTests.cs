using Settlement.API.Security;
using System.Security.Claims;

namespace Settlement.API.Tests;

public class HangfireAdminAuthorizationTests
{
    [Fact]
    public void IsAuthorized_NullUser_ReturnsFalse()
    {
        Assert.False(HangfireAdminAuthorization.IsAuthorized(null));
    }

    [Fact]
    public void IsAuthorized_Anonymous_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(HangfireAdminAuthorization.IsAuthorized(user));
    }

    [Fact]
    public void IsAuthorized_AuthenticatedNonAdmin_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Merchant")], "jwt"));

        Assert.False(HangfireAdminAuthorization.IsAuthorized(user));
    }

    [Fact]
    public void IsAuthorized_AuthenticatedAdmin_ReturnsTrue()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")], "jwt"));

        Assert.True(HangfireAdminAuthorization.IsAuthorized(user));
    }

    [Fact]
    public void IsAuthorized_AdminWithoutAuthn_ReturnsFalse()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Admin")]));

        Assert.False(HangfireAdminAuthorization.IsAuthorized(user));
    }
}
