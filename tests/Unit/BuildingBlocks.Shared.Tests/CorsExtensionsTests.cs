using BuildingBlocks.Shared.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Tests;

public class CorsExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var data = settings.ToDictionary(s => s.Key, s => (string?)s.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static CorsPolicy GetPolicy(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddPaymentSwitchCors(config);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>>();
        return options.Value.GetPolicy(CorsExtensions.AllowFrontendPolicy)!;
    }

    [Fact]
    public void AddPaymentSwitchCors_WithoutConfig_FallsBackToLocalhostDevOrigins()
    {
        var policy = GetPolicy(BuildConfig());

        Assert.Contains("http://localhost:3000", policy.Origins);
        Assert.Contains("http://localhost:5173", policy.Origins);
        Assert.Equal(4, policy.Origins.Count);
    }

    [Fact]
    public void AddPaymentSwitchCors_WithConfig_UsesConfiguredOriginsOnly()
    {
        var policy = GetPolicy(BuildConfig(("Cors:AllowedOrigins", "https://merchant.example.com,https://admin.example.com")));

        Assert.Contains("https://merchant.example.com", policy.Origins);
        Assert.Contains("https://admin.example.com", policy.Origins);
        Assert.Equal(2, policy.Origins.Count);
        Assert.DoesNotContain("http://localhost:3000", policy.Origins);
    }

    [Fact]
    public void AddPaymentSwitchCors_AllowsAnyMethodAndHeader()
    {
        var policy = GetPolicy(BuildConfig(("Cors:AllowedOrigins", "https://merchant.example.com")));

        Assert.True(policy.AllowAnyMethod);
        Assert.True(policy.AllowAnyHeader);
        Assert.False(policy.SupportsCredentials);
    }
}