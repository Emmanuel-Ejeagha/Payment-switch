using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Tests;

public class StartupValidationTests
{
    [Fact]
    public void AddPaymentSwitchCors_InProductionWithoutOrigins_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            })
            .Build();
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddPaymentSwitchCors(config));
        Assert.Contains("AllowedOrigins", ex.Message);
    }

    [Fact]
    public void AddPaymentSwitchCors_InProductionWithHttpOrigin_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Cors:AllowedOrigins:0"] = "http://example.com",
            })
            .Build();
        var services = new ServiceCollection();
        var ex = Assert.Throws<InvalidOperationException>(() => services.AddPaymentSwitchCors(config));
        Assert.Contains("https", ex.Message);
    }

    [Fact]
    public void AddPaymentSwitchCors_InDevelopmentWithoutOrigins_FallsBack()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();
        var services = new ServiceCollection();
        var result = services.AddPaymentSwitchCors(config);
        Assert.NotNull(result);
    }

    [Fact]
    public void RequireGrpcTls_InProductionWithHttp_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            })
            .Build();
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ServiceTokenExtensions.RequireGrpcTls(config, "http://merchant-api:5001"));
        Assert.Contains("https", ex.Message);
    }

    [Fact]
    public void RequireGrpcTls_InProductionWithHttps_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            })
            .Build();
        ServiceTokenExtensions.RequireGrpcTls(config, "https://merchant.internal:5001");
    }

    [Fact]
    public void RequireGrpcTls_InDevelopmentWithHttp_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();
        ServiceTokenExtensions.RequireGrpcTls(config, "http://merchant-api:5001");
    }

    [Fact]
    public void RequireGrpcTls_InProductionWithHttpAndExplicitException_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            })
            .Build();
        ServiceTokenExtensions.RequireGrpcTls(config, "http://merchant-api:5001", allowInsecureException: true);
    }

    [Fact]
    public void RequireGrpcTls_InProductionWithHttpAndNoException_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
            })
            .Build();
        Assert.Throws<InvalidOperationException>(() =>
            ServiceTokenExtensions.RequireGrpcTls(config, "http://merchant-api:5001", allowInsecureException: false));
    }

    [Theory]
    [InlineData("http://merchant-api:5001", true)]
    [InlineData("https://merchant.internal:5001", false)]
    [InlineData("not-a-uri", false)]
    public void ShouldUseInsecureChannel_MatchesScheme(string address, bool expected)
    {
        Assert.Equal(expected, ServiceTokenExtensions.ShouldUseInsecureChannel(address));
    }
}
