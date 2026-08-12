using Ledger.Application.Interfaces;
using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Persistence.Repositories;

public class ReconciliationReportRepository : IReconciliationReportRepository
{
    private readonly AppDbContext _context;

    public ReconciliationReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ReconciliationReport report, CancellationToken cancellationToken = default)
        => await _context.ReconciliationReports.AddAsync(report, cancellationToken);

    public async Task<ReconciliationReport?> GetLatestAsync(CancellationToken cancellationToken = default)
        => await _context.ReconciliationReports
            .AsNoTracking()
            .Include(r => r.Items)
            .OrderByDescending(r => r.RunAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<ReconciliationReport>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
        => await _context.ReconciliationReports
            .AsNoTracking()
            .Include(r => r.Items)
            .OrderByDescending(r => r.RunAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
}