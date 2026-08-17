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
        // Stored emails are normalized to lowercase (Email.cs), so the lookup
        // must normalize the input too or a case-variant query misses its match.
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public async Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.RefreshTokens.Any(t => t.Value == refreshToken))
            .FirstOrDefaultAsync(cancellationToken);
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
        // Stored emails are normalized to lowercase (Email.cs), so compare against
        // the normalized input or a case-variant duplicate slips past this check
        // and surfaces as a unique-index 500 instead of a graceful 409.
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        return await _context.Users.AnyAsync(u => u.Email.Value == normalizedEmail, cancellationToken);
    }

    public Task<int> PruneRefreshTokensAsync(CancellationToken cancellationToken = default)
    {
        // Physically delete tokens that are revoked or already expired so the
        // RefreshTokens table cannot grow without bound. Best-effort cleanup —
        // executed on its own command, independent of the current unit of work.
        return _context.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"RefreshTokens\" WHERE \"IsRevoked\" = true OR \"ExpiresAt\" < NOW()",
            Array.Empty<object>(), cancellationToken);
    }
}