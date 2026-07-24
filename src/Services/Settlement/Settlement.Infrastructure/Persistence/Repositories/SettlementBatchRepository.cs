using Microsoft.EntityFrameworkCore;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;
using Settlement.Domain.Entities;

namespace Settlement.Infrastructure.Persistence.Repositories;

public class SettlementBatchRepository : ISettlementBatchRepository
{
    private readonly AppDbContext _context;

    public SettlementBatchRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SettlementBatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementBatches
            .Include(b => b.Payouts)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<SettlementBatch?> GetByBatchDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        return await _context.SettlementBatches
            .Include(b => b.Payouts)
            .FirstOrDefaultAsync(b => b.BatchDate == date.Date, cancellationToken);
    }

    public async Task AddAsync(SettlementBatch batch, CancellationToken cancellationToken = default)
    {
        await _context.SettlementBatches.AddAsync(batch, cancellationToken);
    }

    public Task UpdateAsync(SettlementBatch batch, CancellationToken cancellationToken = default)
    {
        _context.SettlementBatches.Update(batch);
        return Task.CompletedTask;
    }

    public async Task<List<SettlementBatchDto>> ListAsync(DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _context.SettlementBatches.AsQueryable();

        if (from.HasValue)
            query = query.Where(b => b.BatchDate >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(b => b.BatchDate <= to.Value.Date);

        return await query
            .OrderByDescending(b => b.BatchDate)
            .Skip(skip).Take(take)
            .Select(b => new SettlementBatchDto(
                b.Id,
                b.BatchDate,
                b.Status.Value,
                b.TotalAmount,
                b.Payouts.Select(p => new PayoutDto(
                    p.MerchantId,
                    p.GrossVolume.Amount,
                    p.Fees.Amount,
                    p.NetAmount.Amount,
                    p.Currency
                )).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}