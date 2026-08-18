using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Invoice>> ListBySubscriptionAsync(Guid subscriptionId, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<Invoice>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountBySubscriptionAsync(Guid subscriptionId, Guid merchantId, CancellationToken cancellationToken = default);
    Task<int> CountByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default);

    /// <summary>Finds an already-issued invoice for a billing period, so retries never double-issue.</summary>
    Task<Invoice?> GetBySubscriptionPeriodAsync(Guid subscriptionId, DateTime periodStart, CancellationToken cancellationToken = default);

    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
}
