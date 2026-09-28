using Notification.API.Hubs;
using Notification.API.Services;
using System.Security.Claims;

namespace Notification.API.Tests;

public class HubGroupMembershipTests
{
    [Fact]
    public async Task UnauthenticatedUser_GetsNoGroups()
    {
        var groups = await HubGroupMembership.ResolveAsync(
            new ClaimsPrincipal(new ClaimsIdentity()),
            accessToken: null,
            new StubMerchantGroupResolver(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task AdminRole_GetsAdminGroup()
    {
        var user = UserWithRole("Admin", email: "admin@example.com");

        var groups = await HubGroupMembership.ResolveAsync(
            user,
            accessToken: null,
            new StubMerchantGroupResolver(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(["admin"], groups);
    }

    [Fact]
    public async Task MerchantWithToken_GetsMerchantGroup_FromClaimsAndValidation()
    {
        var merchantId = Guid.NewGuid();
        var user = UserWithRole("MerchantUser", email: "owner@example.com");

        var groups = await HubGroupMembership.ResolveAsync(
            user,
            accessToken: "sk_test_123",
            new StubMerchantGroupResolver(merchantId),
            CancellationToken.None);

        Assert.Equal([$"merchant-{merchantId}"], groups);
    }

    [Fact]
    public async Task MerchantWithoutToken_GetsNoMerchantGroup()
    {
        var user = UserWithRole("MerchantUser", email: "owner@example.com");

        var groups = await HubGroupMembership.ResolveAsync(
            user,
            accessToken: null,
            new StubMerchantGroupResolver(Guid.NewGuid()),
            CancellationToken.None);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task UnresolvedMerchant_AddsNoMerchantGroup()
    {
        var user = UserWithRole("MerchantUser", email: "owner@example.com");

        var groups = await HubGroupMembership.ResolveAsync(
            user,
            accessToken: "sk_test_123",
            new StubMerchantGroupResolver(null),
            CancellationToken.None);

        Assert.Empty(groups);
    }

    [Fact]
    public async Task AdminMerchant_GetsBothGroups()
    {
        var merchantId = Guid.NewGuid();
        var user = UserWithRole("Admin", email: "owner@example.com");

        var groups = await HubGroupMembership.ResolveAsync(
            user,
            accessToken: "sk_test_123",
            new StubMerchantGroupResolver(merchantId),
            CancellationToken.None);

        Assert.Equal(["admin", $"merchant-{merchantId}"], groups);
    }

    private static ClaimsPrincipal UserWithRole(string role, string email)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            ],
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private sealed class StubMerchantGroupResolver(Guid? merchantId) : IMerchantGroupResolver
    {
        public Task<Guid?> ResolveMerchantIdAsync(
            string email, string accessToken, CancellationToken cancellationToken = default)
            => Task.FromResult(merchantId);
    }
}