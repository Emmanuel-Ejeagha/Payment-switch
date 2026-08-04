using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IWebhookEventRepository
{
    Task<List<WebhookEvent>> GetPendingBatchAsync(DateTime before, int batchSize, CancellationToken cancellationToken = default);
    Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<WebhookEvent>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}
