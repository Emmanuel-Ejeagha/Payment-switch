using Identity.Domain.Entities;
using Identity.Domain.ValueObjects;
using Identity.Infrastructure.Services;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.Infrastructure.Tests.Services;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "super_secret_key_that_is_32_bytes_long!",
            Issuer = "test",
            Audience = "test",
            AccessTokenExpirationMinutes = 15
        });
        _tokenService = new TokenService(jwtSettings);
    }

    [Fact]
    public void GenerateAccessToken_ShouldContainClaims()
    {
        var user = new User(Guid.NewGuid(), new Email("test@test.com"), new PasswordHash("hash"), new FullName("Test User"));
        var token = _tokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.NotNull(token);
        Assert.Equal("test@test.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Contains("Merchant", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var token = _tokenService.GenerateRefreshToken();
        Assert.False(string.IsNullOrEmpty(token));
    }
}