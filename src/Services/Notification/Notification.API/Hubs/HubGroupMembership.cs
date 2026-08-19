using Microsoft.AspNetCore.SignalR;
using Notification.API.Services;
using System.Security.Claims;

namespace Notification.API.Hubs;

/// <summary>
/// Resolves the SignalR groups a connection belongs to. Group membership is
/// derived exclusively from the authenticated JWT claims (role + email), with
/// the merchant id validated by the Merchant service against the caller's own
/// bearer token. Clients never supply group names — there is no hub method that
/// accepts one (TASK-047).
/// </summary>
public static class HubGroupMembership
{
    public static async Task<IReadOnlyList<string>> ResolveAsync(
        ClaimsPrincipal? user,
        string? accessToken,
        IMerchantGroupResolver merchantGroupResolver,
        CancellationToken cancellationToken)
    {
        var groups = new List<string>();

        if (user?.Identity?.IsAuthenticated != true)
        {
            return groups;
        }

        if (user.IsInRole("Admin"))
        {
            groups.Add("admin");
        }

        var email = user.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(accessToken))
        {
            var merchantId = await merchantGroupResolver.ResolveMerchantIdAsync(
                email,
                accessToken,
                cancellationToken);

            if (merchantId.HasValue)
            {
                groups.Add($"merchant-{merchantId}");
            }
        }

        return groups;
    }
}