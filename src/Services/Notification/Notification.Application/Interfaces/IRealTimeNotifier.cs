namespace Notification.Application.Interfaces;

public interface IRealTimeNotifier
{
    Task NotifyPaymentEventAsync(Guid merchantId, string eventType, string message, CancellationToken cancellationToken = default);
}