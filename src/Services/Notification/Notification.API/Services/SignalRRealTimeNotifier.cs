using Microsoft.AspNetCore.SignalR;
using Notification.API.Hubs; 
using Notification.Application.Interfaces;

namespace Notification.API.Services;

public class SignalRRealTimeNotifier : IRealTimeNotifier
{
    private readonly IHubContext<PaymentNotificationHub> _hubContext;

    public SignalRRealTimeNotifier(IHubContext<PaymentNotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyPaymentEventAsync(Guid merchantId, string eventType, string message, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"merchant-{merchantId}").SendAsync("PaymentEvent", new
        {
            EventType = eventType,
            Message = message,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);
    }
}