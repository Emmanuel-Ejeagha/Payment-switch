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
        await base.OnConnectedAsync();
    }
}