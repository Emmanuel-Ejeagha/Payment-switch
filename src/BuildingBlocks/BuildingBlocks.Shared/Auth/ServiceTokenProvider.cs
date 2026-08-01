using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Shared.Auth;

public sealed class ServiceTokenProvider
{
    private readonly ServiceTokenOptions _options;
    private readonly object _sync = new();
    private string? _cachedToken;
    private DateTime _cachedExpiryUtc;

    public ServiceTokenProvider(ServiceTokenOptions options)
    {
        _options = options;
    }

    public string GetToken()
    {
        lock (_sync)
        {
            if (_cachedToken is not null && DateTime.UtcNow < _cachedExpiryUtc)
            {
                return _cachedToken;
            }

            var (token, expiresUtc) = CreateToken();
            _cachedToken = token;
            _cachedExpiryUtc = expiresUtc;
            return token;
        }
    }

    private (string Token, DateTime ExpiresUtc) CreateToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var nowUtc = DateTime.UtcNow;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, _options.ServiceName),
            new Claim(ServiceTokenOptions.ClientTypeClaim, ServiceTokenOptions.ClientTypeService),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var securityToken = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: nowUtc.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        var token = new JwtSecurityTokenHandler().WriteToken(securityToken);
        return (token, securityToken.ValidTo.ToUniversalTime());
    }
}
