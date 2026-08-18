using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<List<Invoice>> ListBySubscriptionAsync(Guid subscriptionId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.SubscriptionId == subscriptionId)
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invoice>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.MerchantId == merchantId)
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountBySubscriptionAsync(Guid subscriptionId, Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .CountAsync(i => i.SubscriptionId == subscriptionId && i.MerchantId == merchantId, cancellationToken);
    }

    public async Task<int> CountByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .CountAsync(i => i.MerchantId == merchantId, cancellationToken);
    }

    public async Task<Invoice?> GetBySubscriptionPeriodAsync(Guid subscriptionId, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .FirstOrDefaultAsync(i => i.SubscriptionId == subscriptionId && i.PeriodStart == periodStart, cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
    }
}
