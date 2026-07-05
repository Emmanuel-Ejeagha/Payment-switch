using Merchant.Application.DTOs;
using Merchant.Application.Interfaces;
using Merchant.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Merchant.Infrastructure.Persistence.Repositories;

public class MerchantRepository : IMerchantRepository
{
    private readonly AppDbContext _context;

    public MerchantRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MerchantEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Merchants.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<MerchantEntity?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLowerInvariant();
        return await _context.Merchants.FirstOrDefaultAsync(m => m.Email.Value == normalized, cancellationToken);
    }

    public async Task AddAsync(MerchantEntity merchant, CancellationToken cancellationToken = default)
    {
        await _context.Merchants.AddAsync(merchant, cancellationToken);
    }

    public Task UpdateAsync(MerchantEntity merchant, CancellationToken cancellationToken = default)
    {
        _context.Merchants.Update(merchant);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.ToLowerInvariant();
        return await _context.Merchants.AnyAsync(m => m.Email.Value == normalized, cancellationToken);
    }

    public async Task<List<MerchantDto>> ListAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Merchants
            .OrderBy(m => m.CreatedAt)
            .Skip(skip).Take(take)
            .Select(m => new MerchantDto(m.Id, m.BusinessName.Value, m.Email.Value, m.Status.Value, m.WebhookUrl == null ? null : m.WebhookUrl.Value, m.EnabledPaymentMethods.ToList()))
            .ToListAsync(cancellationToken);
    }
}