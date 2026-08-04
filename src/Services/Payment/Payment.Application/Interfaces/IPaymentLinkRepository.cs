using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentLinkRepository
{
    Task<PaymentLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<List<PaymentLink>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentLink link, CancellationToken cancellationToken = default);
}
