using Microsoft.EntityFrameworkCore;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Repositories;

public class PaymentIntentRepository : IPaymentIntentRepository
{
    private readonly AppDbContext _context;

    public PaymentIntentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentIntent?> GetByIdAsync(Guid intentId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentIntents
            .Include(p => p.Transactions)
            .FirstOrDefaultAsync(p => p.Id == intentId, cancellationToken);
    }

    public async Task<PaymentIntent?> GetByIdempotencyKeyAsync(Guid merchantId, string idempotencyKey, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentIntents
            .FirstOrDefaultAsync(p => p.MerchantId == merchantId && p.IdempotencyKey.Value == idempotencyKey, cancellationToken);
    }

    public async Task AddAsync(PaymentIntent intent, CancellationToken cancellationToken = default)
    {
        await _context.PaymentIntents.AddAsync(intent, cancellationToken);
    }

    public Task UpdateAsync(PaymentIntent intent, CancellationToken cancellationToken = default)
    {
        _context.PaymentIntents.Update(intent);
        return Task.CompletedTask;
    }

    public async Task<List<PaymentIntentDto>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentIntents
            .Where(p => p.MerchantId == merchantId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip).Take(take)
            .Select(p => new PaymentIntentDto(
                p.Id,
                p.MerchantId,
                p.Amount.Amount,
                p.Amount.Currency,
                p.Status.Value,
                p.Transactions.Select(t => new TransactionDto(
                    t.Id,
                    t.Type.ToString(),
                    t.Amount.Amount,
                    t.Amount.Currency,
                    t.Timestamp
                )).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}