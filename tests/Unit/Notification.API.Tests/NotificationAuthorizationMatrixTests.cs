using BuildingBlocks.Shared.Auth;
using Microsoft.AspNetCore.Authorization;
using Notification.API.Controllers;
using System.Reflection;

namespace Notification.API.Tests;

/// <summary>
/// Step 7.5: Support = read-only. Notification reads admit Admin + Support.
/// </summary>
public class NotificationAuthorizationMatrixTests
{
    private static string RolesOf(MethodInfo method) =>
        method.GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? typeof(NotificationsController).GetCustomAttribute<AuthorizeAttribute>()?.Roles
        ?? string.Empty;

    [Theory]
    [InlineData(nameof(NotificationsController.GetById))]
    [InlineData(nameof(NotificationsController.List))]
    public void Reads_AdmitAdminAndSupport(string action)
    {
        Assert.Equal(RolePolicies.ReadOnly, RolesOf(typeof(NotificationsController).GetMethod(action)!));
    }
}
