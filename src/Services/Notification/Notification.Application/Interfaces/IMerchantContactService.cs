namespace Notification.Application.Interfaces;

public interface IMerchantContactService
{
    Task<string?> GetMerchantEmailAsync(Guid merchantId, CancellationToken cancellationToken = default);
}
