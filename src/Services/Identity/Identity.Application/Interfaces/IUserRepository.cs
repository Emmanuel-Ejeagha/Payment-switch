using Identity.Application.DTOs;
using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithApiKeysAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> RevokeApiKeyAsync(Guid userId, Guid keyId, CancellationToken cancellationToken = default);
    Task AddApiKeyAsync(User user, ApiKey apiKey, CancellationToken cancellationToken = default);
    Task<List<ApiKeyDto>> GetApiKeysByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
