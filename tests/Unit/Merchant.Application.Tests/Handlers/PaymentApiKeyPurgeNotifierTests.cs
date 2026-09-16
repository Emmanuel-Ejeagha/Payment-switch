using BuildingBlocks.Shared.Auth;
using Merchant.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;

namespace Merchant.Application.Tests.Handlers;

/// <summary>
/// Step 7.4: the purge notify is best-effort — correct request shape on
/// success, and never throws (the short cache TTL backstops a miss).
/// </summary>
public class PaymentApiKeyPurgeNotifierTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Seen;
        public string? SeenBody;
        public Func<HttpRequestMessage, HttpResponseMessage> Respond { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Seen = request;
            SeenBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return Respond(request);
        }
    }

    private sealed class TestOptions : IOptionsMonitor<PaymentPurgeOptions>
    {
        public PaymentPurgeOptions CurrentValue { get; } = new() { BaseUrl = "http://payment-test:8080" };
        public PaymentPurgeOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<PaymentPurgeOptions, string?> listener) => null;
    }

    private static ServiceTokenProvider TokenProvider() => new(new ServiceTokenOptions
    {
        ServiceName = "Merchant",
        Issuer = "IdentityService",
        Audience = "PaymentSwitch",
        Secret = "merchant-service-token-secret-32-chars!!"
    });

    private static PaymentApiKeyPurgeNotifier Create(StubHandler handler) =>
        new(new HttpClient(handler), TokenProvider(), new TestOptions(),
            NullLogger<PaymentApiKeyPurgeNotifier>.Instance);

    [Fact]
    public async Task NotifyRevoked_PostsMerchantIdWithServiceToken()
    {
        var handler = new StubHandler();
        var merchantId = Guid.NewGuid();

        await Create(handler).NotifyRevokedAsync(merchantId);

        Assert.NotNull(handler.Seen);
        Assert.Equal(HttpMethod.Post, handler.Seen!.Method);
        Assert.Equal(
            "http://payment-test:8080/api/v1/internal/apikeys/purge",
            handler.Seen.RequestUri!.ToString());
        Assert.StartsWith("Bearer ", handler.Seen.Headers.Authorization!.ToString());
        Assert.Contains(merchantId.ToString(), handler.SeenBody);
    }

    [Fact]
    public async Task NotifyRevoked_ServerError_DoesNotThrow()
    {
        var handler = new StubHandler
        {
            Respond = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };

        await Create(handler).NotifyRevokedAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task NotifyRevoked_NetworkError_DoesNotThrow()
    {
        var handler = new StubHandler
        {
            Respond = _ => throw new HttpRequestException("connection refused")
        };

        await Create(handler).NotifyRevokedAsync(Guid.NewGuid());
    }
}
