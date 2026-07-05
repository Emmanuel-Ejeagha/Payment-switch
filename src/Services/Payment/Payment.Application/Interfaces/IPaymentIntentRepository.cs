using Payment.Application.DTOs;
using Payment.Domain;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentIntentRepository
{
    Task<PaymentIntent?> GetByIdAsync(Guid intentId, CancellationToken cancellationToken = default);
    Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid merchantId, string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default);
    Task UpdateAsync(PaymentIntent intent, CancellationToken cancellationToken = default);
    Task<List<PaymentIntentDto>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
}