using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Plan>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
}
