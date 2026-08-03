using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Notification.API.Services;
using System.Security.Claims;

namespace Notification.API.Hubs;

[Authorize]
public class PaymentNotificationHub : Hub
{
    private readonly IMerchantGroupResolver _merchantGroupResolver;

    public PaymentNotificationHub(IMerchantGroupResolver merchantGroupResolver)
    {
        _merchantGroupResolver = merchantGroupResolver;
    }

    public override async Task OnConnectedAsync()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            if (user.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
            }

            var email = user.FindFirstValue(ClaimTypes.Email);
            var accessToken = Context.GetHttpContext()?.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(accessToken))
            {
                var merchantId = await _merchantGroupResolver.ResolveMerchantIdAsync(
                    email,
                    accessToken.ToString(),
                    Context.ConnectionAborted);

                if (merchantId.HasValue)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"merchant-{merchantId}");
                }
            }
        }

        await base.OnConnectedAsync();
    }
}
