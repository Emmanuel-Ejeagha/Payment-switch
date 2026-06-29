namespace Merchant.Application.Interfaces;

public interface IMerchantRepository
{
    Task<MerchantEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MerchantEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(MerchantEntity merchant, CancellationToken cancellationToken = default);
    Task UpdateAsync(MerchantEntity merchant, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<MerchantDto>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}