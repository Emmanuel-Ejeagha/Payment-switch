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
        var normalized = date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date;
        return await _context.SettlementBatches
            .Include(b => b.Payouts)
            .FirstOrDefaultAsync(b => b.BatchDate == normalized.Date, cancellationToken);
    }

    public async Task AddAsync(SettlementBatch batch, CancellationToken cancellationToken = default)
    {
        await _context.SettlementBatches.AddAsync(batch, cancellationToken);
    }

    public async Task<List<SettlementBatchDto>> ListAsync(DateTime? from, DateTime? to, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = ApplyDateFilter(_context.SettlementBatches.AsQueryable(), from, to);

        var batches = await query
            .OrderByDescending(b => b.BatchDate)
            .Skip(skip).Take(take)
            .Include(b => b.Payouts)
            .ToListAsync(cancellationToken);

        return batches.Select(b => new SettlementBatchDto(
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
        )).ToList();
    }

    public async Task<int> CountAsync(DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
    {
        var query = ApplyDateFilter(_context.SettlementBatches.AsQueryable(), from, to);
        return await query.CountAsync(cancellationToken);
    }

    private static IQueryable<SettlementBatch> ApplyDateFilter(IQueryable<SettlementBatch> query, DateTime? from, DateTime? to)
    {
        if (from.HasValue)
        {
            var fromDate = from.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(from.Value, DateTimeKind.Utc) : from.Value;
            query = query.Where(b => b.BatchDate >= fromDate.Date);
        }
        if (to.HasValue)
        {
            var toDate = to.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : to.Value;
            query = query.Where(b => b.BatchDate <= toDate.Date);
        }
        return query;
    }
}