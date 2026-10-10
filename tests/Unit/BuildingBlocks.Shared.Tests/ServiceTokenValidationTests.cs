using BuildingBlocks.Shared.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BuildingBlocks.Shared.Tests;

public class ServiceTokenValidationTests
{
    private const string ServiceSecret = "service-token-test-secret-32-bytes!";
    private const string UserSecret = "user-jwt-test-secret-0123456789ab";

    private static IConfiguration Config(string? serviceSecret = ServiceSecret, string? userSecret = UserSecret)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ServiceToken:Secret"] = serviceSecret,
                ["ServiceToken:ServiceName"] = "Payment",
                ["Jwt:Secret"] = userSecret,
                ["Jwt:Issuer"] = "IdentityService",
                ["Jwt:Audience"] = "PaymentSwitch",
            })
            .Build();

    private static TokenValidationParameters ServiceSchemeParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ValidIssuer = "IdentityService",
        ValidAudience = "PaymentSwitch",
        IssuerSigningKeys = new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ServiceSecret)) },
    };

    private static TokenValidationParameters UserSchemeParameters() => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
        ValidIssuer = "IdentityService",
        ValidAudience = "PaymentSwitch",
        IssuerSigningKeys = new[] { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(UserSecret)) },
    };

    private static string MintServiceToken()
    {
        var services = new ServiceCollection();
        services.AddServiceTokenProvider(Config(), "Payment");
        var provider = services.BuildServiceProvider().GetRequiredService<ServiceTokenProvider>();
        return provider.GetToken();
    }

    [Fact]
    public void ServiceToken_ValidatesUnderServiceScheme_WithServiceClaim()
    {
        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(MintServiceToken(), ServiceSchemeParameters(), out _);

        Assert.Equal("service", principal.FindFirst(ServiceTokenOptions.ClientTypeClaim)?.Value);
    }

    [Fact]
    public void ServiceToken_RejectedUnderUserScheme_WrongKey()
    {
        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(MintServiceToken(), UserSchemeParameters(), out _));
    }

    [Fact]
    public void AddServiceTokenProvider_MissingSecret_Throws()
    {
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddServiceTokenProvider(Config(serviceSecret: null), "Payment"));
        Assert.Contains("ServiceToken:Secret", ex.Message);
    }

    [Fact]
    public void AddServiceTokenProvider_SecretEqualToJwtSecret_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddServiceTokenProvider(Config(serviceSecret: UserSecret), "Payment"));
    }
}
