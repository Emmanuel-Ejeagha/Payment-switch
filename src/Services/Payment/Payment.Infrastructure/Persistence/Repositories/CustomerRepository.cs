using Microsoft.EntityFrameworkCore;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.MerchantId == merchantId && c.Email == email && !c.Deleted, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Guid merchantId, string email, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .AnyAsync(c => c.MerchantId == merchantId && c.Email == email && !c.Deleted, cancellationToken);
    }

    public async Task<List<Customer>> ListByMerchantAsync(Guid merchantId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .Where(c => c.MerchantId == merchantId && !c.Deleted)
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByMerchantAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .CountAsync(c => c.MerchantId == merchantId && !c.Deleted, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }
}
