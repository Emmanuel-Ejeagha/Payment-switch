using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Payment.API.IntegrationTests;

public class SwaggerExposureTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public SwaggerExposureTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Swagger_IsEnabledInNonProduction()
    {
        var client = _factory.CreateClient();

        var ui = await client.GetAsync("/swagger");
        var json = await client.GetAsync("/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, ui.StatusCode);
        Assert.Equal(HttpStatusCode.OK, json.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsDisabledInProduction()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = production.CreateClient();

        var ui = await client.GetAsync("/swagger");
        var json = await client.GetAsync("/v1/swagger.json");

        Assert.Equal(HttpStatusCode.NotFound, ui.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, json.StatusCode);
    }

    [Fact]
    public async Task Metrics_RemainsReachableInProduction()
    {
        using var production = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        var client = production.CreateClient();

        var response = await client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}