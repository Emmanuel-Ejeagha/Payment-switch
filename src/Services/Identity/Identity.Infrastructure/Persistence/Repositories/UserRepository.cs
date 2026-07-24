using Identity.Application.DTOs;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByIdWithApiKeysAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.ApiKeys)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task AddApiKeyAsync(User user, ApiKey apiKey, CancellationToken cancellationToken = default)
    {
        _context.ApiKeys.Add(apiKey);
        _context.Entry(apiKey).Property("UserId").CurrentValue = user.Id;
        return Task.CompletedTask;
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var users = await _context.Users.ToListAsync(cancellationToken);
        return users.FirstOrDefault(u => u.RefreshTokens.Any(t => t.Value == refreshToken));
    }

    public async Task<List<ApiKeyDto>> GetApiKeysByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.ApiKeys
            .Where(k => EF.Property<Guid>(k, "UserId") == userId)
            .Select(k => new ApiKeyDto(k.Id, k.Environment, k.CreatedAt, k.RevokedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevokeApiKeyAsync(Guid userId, Guid keyId, CancellationToken cancellationToken = default)
    {
        var apiKey = await _context.ApiKeys
        .FirstOrDefaultAsync(k => k.Id == keyId && EF.Property<Guid>(k, "UserId") == userId, cancellationToken);

        if (apiKey is null)
            return false;

        apiKey.Revoke();

        _context.Entry(apiKey).State = EntityState.Modified;

        return true;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users.AnyAsync(u => u.Email.Value == email, cancellationToken);
    }
}