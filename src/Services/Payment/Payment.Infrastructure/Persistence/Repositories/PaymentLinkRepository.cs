using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Repositories;

public class PaymentLinkRepository : IPaymentLinkRepository
{
    private readonly AppDbContext _context;

    public PaymentLinkRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentLinks
            .FirstOrDefaultAsync(l => l.Code == code, cancellationToken);
    }

    public async Task<List<PaymentLink>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentLinks
            .Where(l => l.MerchantId == merchantId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PaymentLink link, CancellationToken cancellationToken = default)
    {
        await _context.PaymentLinks.AddAsync(link, cancellationToken);
    }
}
