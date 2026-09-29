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

    [Fact]
    public void IsAuthorized_LowercaseAdminClaim_ReturnsTrue()
    {
        // Step 7.5: claims checks are case-insensitive (legacy tokens).
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "admin")], "jwt"));

        Assert.True(HangfireAdminAuthorization.IsAuthorized(user));
    }

    [Fact]
    public void IsAuthorized_Support_ReturnsFalse()
    {
        // Step 7.5: Support is read-only — no Hangfire job control.
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Support")], "jwt"));

        Assert.False(HangfireAdminAuthorization.IsAuthorized(user));
    }
}
