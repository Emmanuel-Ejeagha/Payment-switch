using Microsoft.AspNetCore.SignalR;

namespace Notification.API.Hubs;

public class PaymentNotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var merchantId = httpContext?.Request.Query["merchantId"];
        if (!string.IsNullOrEmpty(merchantId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"merchant-{merchantId}");
        }
        var isAdmin = httpContext?.Request.Query["isAdmin"].FirstOrDefault();
        if (!string.IsNullOrEmpty(isAdmin) && isAdmin == "true")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
        }
        await base.OnConnectedAsync();
    }
}