using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Persistence.Repositories;

public class LedgerAccountRepository : ILedgerAccountRepository
{
    private readonly AppDbContext _context;

    public LedgerAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LedgerAccount?> GetByMerchantIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.LedgerAccounts
            .Include(a => a.Journal)
            .FirstOrDefaultAsync(a => a.MerchantId == merchantId, cancellationToken);
    }

    public async Task AddAsync(LedgerAccount account, CancellationToken cancellationToken = default)
    {
        await _context.LedgerAccounts.AddAsync(account, cancellationToken);
    }

    public Task UpdateAsync(LedgerAccount account, CancellationToken cancellationToken = default)
    {
        _context.LedgerAccounts.Update(account);
        return Task.CompletedTask;
    }
}