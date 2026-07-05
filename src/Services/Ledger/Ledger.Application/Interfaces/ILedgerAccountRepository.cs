using Ledger.Domain.Entities;

namespace Ledger.Application.Interfaces;

public interface ILedgerAccountRepository
{
    Task<LedgerAccount?> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task AddAsync(LedgerAccount account, CancellationToken cancellationToken = default);
    Task UpdateAsync(LedgerAccount account, CancellationToken cancellationToken = default);
}