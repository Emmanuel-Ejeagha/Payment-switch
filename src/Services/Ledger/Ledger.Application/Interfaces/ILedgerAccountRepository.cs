using Ledger.Domain.Entities;

namespace Ledger.Application.Interfaces;

public interface ILedgerAccountRepository
{
    Task<LedgerAccount?> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<LedgerAccount?> GetByMerchantIdAndCurrencyAsync(Guid merchantId, string currency, CancellationToken cancellationToken = default);
    Task<List<LedgerAccount>> ListByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task AddAsync(LedgerAccount account, CancellationToken cancellationToken = default);
}