using BuildingBlocks.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Settlement.API.Controllers;
using System.Reflection;

namespace Settlement.API.Tests;

/// <summary>
/// Step 7.5: Support = read-only. Money-moving trigger stays Admin-only;
/// batch reads admit Admin + Support.
/// </summary>
public class SettlementAuthorizationMatrixTests
{
    private static string RolesOf(MethodInfo method) =>
        method.GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? typeof(SettlementController).GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? string.Empty;

    [Fact]
    public void Trigger_RequiresAdminOnly()
    {
        Assert.Equal(RolePolicies.AdminOnly, RolesOf(typeof(SettlementController).GetMethod(nameof(SettlementController.Trigger))!));
    }

    [Theory]
    [InlineData(nameof(SettlementController.GetById))]
    [InlineData(nameof(SettlementController.List))]
    public void Reads_AdmitAdminAndSupport(string action)
    {
        Assert.Equal(RolePolicies.ReadOnly, RolesOf(typeof(SettlementController).GetMethod(action)!));
    }
}
