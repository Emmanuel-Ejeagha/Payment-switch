using BuildingBlocks.Shared.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace Payment.API.Tests;

/// <summary>
/// Step 7.4: the Strict policy (20/10s) rejects the 21st request with 429 per
/// client, and one client's exhaustion does not affect another client.
/// Hosted on loopback Kestrel — no containers required.
/// </summary>
public class StrictRateLimitTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private string _baseUrl = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddPaymentSwitchRateLimiting();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        // Test-only: let each case pick its client IP (production uses the
        // ForwardedHeaders-restored RemoteIpAddress).
        _app.Use(async (ctx, next) =>
        {
            if (ctx.Request.Headers.TryGetValue("X-Test-Ip", out var ip) &&
                IPAddress.TryParse(ip.ToString(), out var parsed))
                ctx.Connection.RemoteIpAddress = parsed;
            await next(ctx);
        });
        _app.UseRateLimiter();
        _app.MapGet("/strict", () => Results.Ok()).RequireRateLimiting("Strict");
        await _app.StartAsync();
        _baseUrl = _app.Urls.First();
        _client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
    }

    private async Task<HttpStatusCode> GetAsync(string ip)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/strict");
        request.Headers.Add("X-Test-Ip", ip);
        using var response = await _client!.SendAsync(request);
        return response.StatusCode;
    }

    [Fact]
    public async Task StrictPolicy_TwentyFirstRequestInWindow_Is429()
    {
        for (var i = 0; i < 20; i++)
            Assert.Equal(HttpStatusCode.OK, await GetAsync("10.20.0.1"));

        Assert.Equal(HttpStatusCode.TooManyRequests, await GetAsync("10.20.0.1"));
    }

    [Fact]
    public async Task StrictPolicy_ExhaustedClient_DoesNotAffectOtherClient()
    {
        for (var i = 0; i < 21; i++)
            await GetAsync("10.20.0.2");

        Assert.Equal(HttpStatusCode.OK, await GetAsync("10.20.0.3"));
    }
}
