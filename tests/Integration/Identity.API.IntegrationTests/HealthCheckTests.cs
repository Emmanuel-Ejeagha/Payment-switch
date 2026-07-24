using System.Net.Http.Json;

namespace Identity.API.IntegrationTests;

public class HealthCheckTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Liveness_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_Returns503_WhenRabbitMqUnavailable()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
