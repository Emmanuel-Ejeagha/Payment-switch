using BuildingBlocks.Shared.Http;
using BuildingBlocks.Shared.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Payment.API.Tests;

/// <summary>
/// Step 8.2: the app emits HSTS whenever it knows the outer scheme is https
/// (here via X-Forwarded-Proto, as nginx sends), and the frame policy matches
/// the edge (SAMEORIGIN + frame-ancestors 'self'). Hosted on loopback
/// Kestrel — DefaultHttpContext never fires OnStarting, so this needs the
/// real pipeline.
/// </summary>
public class SecurityHeadersBehaviorTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        _app.UsePaymentSwitchForwardedHeaders(builder.Configuration);
        _app.UsePaymentSwitchSecurityHeaders();
        _app.MapGet("/", () => Results.Ok());
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Fact]
    public async Task PlainHttp_NoHsts_ButBaselineHeadersPresent()
    {
        using var response = await _client!.GetAsync("/");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.False(response.Headers.Contains("Strict-Transport-Security"));
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
    }

    [Fact]
    public async Task ForwardedHttps_EmitsHsts()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("X-Forwarded-Proto", "https");
        using var response = await _client!.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("max-age=31536000; includeSubDomains",
            response.Headers.GetValues("Strict-Transport-Security").Single());
        Assert.Equal("SAMEORIGIN", response.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Csp_ContainsFrameAncestorsSelf()
    {
        using var response = await _client!.GetAsync("/");

        var csp = response.Headers.GetValues("Content-Security-Policy").Single();
        Assert.Contains("frame-ancestors 'self'", csp);
    }
}
