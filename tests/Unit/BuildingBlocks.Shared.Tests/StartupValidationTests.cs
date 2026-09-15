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
}
