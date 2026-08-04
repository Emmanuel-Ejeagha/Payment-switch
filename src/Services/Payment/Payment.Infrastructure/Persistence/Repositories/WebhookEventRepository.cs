using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Persistence;

namespace Payment.Infrastructure.Persistence.Repositories;

public class WebhookEventRepository : IWebhookEventRepository
{
    private readonly AppDbContext _context;

    public WebhookEventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<WebhookEvent>> GetPendingBatchAsync(DateTime before, int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Where(e => e.Status == WebhookEvent.StatusPending && e.NextAttemptAt <= before)
            .OrderBy(e => e.NextAttemptAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<WebhookEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<List<WebhookEvent>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.WebhookEvents
            .Where(e => e.MerchantId == merchantId)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip).Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        await _context.WebhookEvents.AddAsync(webhookEvent, cancellationToken);
    }
}
