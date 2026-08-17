using Identity.Domain.Entities;

namespace Identity.Application.Interfaces;

public interface ITokenService
{
    int AccessTokenExpirationSeconds { get; }
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
