using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken = default);
    Task<List<Customer>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
