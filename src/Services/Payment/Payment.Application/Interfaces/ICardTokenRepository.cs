using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface ICardTokenRepository
{
    Task<CardToken?> GetByTokenAsync(Guid merchantId, string token, CancellationToken cancellationToken = default);
    Task AddAsync(CardToken cardToken, CancellationToken cancellationToken = default);
}
