using Hangfire.Dashboard;
using System.Security.Claims;

namespace Settlement.API.Security;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated users holding the Admin
/// role (TASK-013). Runs after <c>UseAuthentication</c>, so the request's
/// claims are populated from the JWT bearer token attached by the frontend BFF
/// proxy. Unauthenticated or non-admin callers get a 401 from Hangfire.
/// </summary>
public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return HangfireAdminAuthorization.IsAuthorized(context.GetHttpContext().User);
    }
}

public static class HangfireAdminAuthorization
{
    public static bool IsAuthorized(ClaimsPrincipal? user)
    {
        return user?.Identity?.IsAuthenticated == true
            && user.IsInRole("Admin");
    }
}
