using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Infrastructure.Persistence.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly AppDbContext _context;

    public SubscriptionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Subscription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<Subscription>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.MerchantId == merchantId)
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .CountAsync(s => s.MerchantId == merchantId, cancellationToken);
    }

    public async Task<List<Subscription>> ListByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Subscriptions
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Subscription>> GetDueBatchAsync(DateTime asOf, int batchSize, CancellationToken cancellationToken = default)
    {
        // Cancel() clears NextBillingAt, so a null check already excludes canceled subscriptions.
        return await _context.Subscriptions
            .Where(s => s.NextBillingAt != null && s.NextBillingAt <= asOf)
            .OrderBy(s => s.NextBillingAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        await _context.Subscriptions.AddAsync(subscription, cancellationToken);
    }
}
