using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Shared.Auth;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Shared.Tests;

public class ServiceTokenProviderTests
{
    private static readonly ServiceTokenOptions Options = new()
    {
        ServiceName = "Payment",
        ExpiryMinutes = 10,
        Issuer = "IdentityService",
        Audience = "PaymentSwitch",
        Secret = "test-super-secret-key-minimum-32-bytes!!"
    };

    [Fact]
    public void GetToken_ReturnsJwtWithServiceClaim()
    {
        var provider = new ServiceTokenProvider(Options);

        var token = provider.GetToken();

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(token));

        var jwt = handler.ReadJwtToken(token);
        Assert.Equal(Options.Issuer, jwt.Issuer);
        Assert.Equal(Options.Audience, jwt.Audiences.Single());
        Assert.Equal(Options.ServiceName, jwt.Subject);
        Assert.Equal(ServiceTokenOptions.ClientTypeService,
            jwt.Claims.Single(c => c.Type == ServiceTokenOptions.ClientTypeClaim).Value);
    }

    [Fact]
    public void GetToken_IsCachedWithinExpiry()
    {
        var provider = new ServiceTokenProvider(Options);

        var first = provider.GetToken();
        var second = provider.GetToken();

        Assert.Equal(first, second);
    }

    [Fact]
    public void GetToken_ValidatesAgainstServerStyleParameters()
    {
        var provider = new ServiceTokenProvider(Options);
        var token = provider.GetToken();

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret))
        };

        var principal = handler.ValidateToken(token, parameters, out _);

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.True(principal.HasClaim(ServiceTokenOptions.ClientTypeClaim, ServiceTokenOptions.ClientTypeService));
        Assert.True(principal.HasClaim(ClaimTypes.NameIdentifier, Options.ServiceName));
    }
}
