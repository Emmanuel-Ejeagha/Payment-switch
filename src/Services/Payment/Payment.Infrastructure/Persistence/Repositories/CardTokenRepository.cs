using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Repositories;

public class CardTokenRepository : ICardTokenRepository
{
    private readonly AppDbContext _context;

    public CardTokenRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CardToken?> GetByTokenAsync(Guid merchantId, string token, CancellationToken cancellationToken = default)
    {
        return await _context.CardTokens
            .FirstOrDefaultAsync(t => t.MerchantId == merchantId && t.Token == token, cancellationToken);
    }

    public async Task AddAsync(CardToken cardToken, CancellationToken cancellationToken = default)
    {
        await _context.CardTokens.AddAsync(cardToken, cancellationToken);
    }
}
