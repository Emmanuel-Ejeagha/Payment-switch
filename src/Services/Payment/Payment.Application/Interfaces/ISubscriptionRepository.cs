using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Subscription>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<Subscription>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscriptions whose next billing date has arrived, ordered oldest first.
    /// Drives the recurring-charge worker.
    /// </summary>
    Task<List<Subscription>> GetDueBatchAsync(DateTime asOf, int batchSize, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
