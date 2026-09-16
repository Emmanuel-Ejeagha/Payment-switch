using BuildingBlocks.Shared.Auth;
using Merchant.API.Controllers;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Merchant.API.Tests;

/// <summary>
/// Step 7.5: Support = read-only. Merchant state transitions stay Admin-only;
/// the merchant list admits Admin + Support.
/// </summary>
public class MerchantAuthorizationMatrixTests
{
    private static string RolesOf(MethodInfo method) =>
        method.GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? typeof(MerchantsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? string.Empty;

    [Theory]
    [InlineData(nameof(MerchantsController.Activate))]
    [InlineData(nameof(MerchantsController.Approve))]
    [InlineData(nameof(MerchantsController.Reject))]
    [InlineData(nameof(MerchantsController.Suspend))]
    [InlineData(nameof(MerchantsController.Reactivate))]
    public void StateTransitions_RequireAdminOnly(string action)
    {
        Assert.Equal(RolePolicies.AdminOnly, RolesOf(typeof(MerchantsController).GetMethod(action)!));
    }

    [Fact]
    public void List_AdmitsAdminAndSupport()
    {
        Assert.Equal(RolePolicies.ReadOnly, RolesOf(typeof(MerchantsController).GetMethod(nameof(MerchantsController.List))!));
    }
}
