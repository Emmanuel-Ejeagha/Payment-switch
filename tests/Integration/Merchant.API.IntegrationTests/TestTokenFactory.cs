using BuildingBlocks.Shared.Auth;
using Microsoft.IdentityModel.Tokens;
using PaymentSwitch.IntegrationTests.Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Merchant.API.IntegrationTests;

/// <summary>
/// Mints user JWTs that the real JwtBearer pipeline accepts, so tests can
/// exercise owner-scoped endpoints (e.g. merchant onboarding, TASK-038).
/// </summary>
internal static class TestTokenFactory
{
    public static string CreateUserToken(Guid userId, string email, bool emailVerified, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(CustomClaimTypes.EmailVerified, emailVerified ? "true" : "false")
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecrets.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "MerchantService",
            audience: TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
