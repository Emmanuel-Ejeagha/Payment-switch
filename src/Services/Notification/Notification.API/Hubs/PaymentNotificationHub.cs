using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Notification.API.Services;

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
        var accessToken = Context.GetHttpContext()?.Request.Query["access_token"];
        var groups = await HubGroupMembership.ResolveAsync(
            Context.User,
            accessToken,
            _merchantGroupResolver,
            Context.ConnectionAborted);

        foreach (var group in groups)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, group);
        }

        await base.OnConnectedAsync();
    }
}