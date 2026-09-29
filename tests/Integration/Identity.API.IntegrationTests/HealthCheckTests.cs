using System.Net;
using System.Net.Http.Json;

namespace Identity.API.IntegrationTests;

public class HealthCheckTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
        // If readiness hangs on a dead broker the test fails fast instead of
        // waiting on the default 100s HttpClient timeout.
        _client.Timeout = TimeSpan.FromSeconds(10);
    }

    [Fact]
    public async Task Liveness_Returns200()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Ready_Returns503_WhenRabbitMqUnavailable()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await _client.GetAsync("/health/ready");
        sw.Stop();

        // The app stays alive and reports readiness while a dependency is down,
        // and the probe answers promptly instead of blocking on a TCP connect.
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5), $"ready took {sw.Elapsed}");
    }
}