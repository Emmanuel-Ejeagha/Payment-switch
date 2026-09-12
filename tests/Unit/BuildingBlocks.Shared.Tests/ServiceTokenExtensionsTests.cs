using BuildingBlocks.Shared.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace BuildingBlocks.Shared.Tests;

public class ServiceTokenExtensionsTests
{
    private const string JwtSecret = "jwt-secret-key-at-least-32-chars-long!!!";
    private const string ServiceSecret = "service-secret-key-at-least-32-chars-lo!";

    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var data = new Dictionary<string, string?>();
        foreach (var (key, value) in settings)
            data[key] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static ServiceTokenProvider ResolveProvider(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddServiceTokenProvider(config, "Payment");
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ServiceTokenProvider>();
    }

    [Fact]
    public void AddServiceTokenProvider_WithMissingSecret_Throws()
    {
        var config = BuildConfig(("Jwt:Secret", JwtSecret));

        var ex = Assert.Throws<InvalidOperationException>(() => ResolveProvider(config));
        Assert.Contains("ServiceToken:Secret", ex.Message);
    }

    [Fact]
    public void AddServiceTokenProvider_WithSecretEqualToJwtSecret_Throws()
    {
        var config = BuildConfig(
            ("Jwt:Secret", JwtSecret),
            ("ServiceToken:Secret", JwtSecret));

        var ex = Assert.Throws<InvalidOperationException>(() => ResolveProvider(config));
        Assert.Contains("must differ from Jwt:Secret", ex.Message);
    }

    [Fact]
    public void AddServiceTokenProvider_WithDistinctSecret_MintsVerifiableToken()
    {
        var config = BuildConfig(
            ("Jwt:Secret", JwtSecret),
            ("ServiceToken:Secret", ServiceSecret),
            ("ServiceToken:Issuer", "IdentityService"),
            ("ServiceToken:Audience", "PaymentSwitch"));

        var token = ResolveProvider(config).GetToken();

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "IdentityService",
            ValidAudience = "PaymentSwitch",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ServiceSecret))
        }, out _);

        Assert.True(principal.Identity!.IsAuthenticated);
    }
}
