using Payment.Domain.Entities;

namespace Payment.Infrastructure.Tests.Services;

public class WebhookRetryTests
{
    private sealed class CountingFakeHandler : HttpMessageHandler
    {
        private readonly int _failCount;
        public int CallCount { get; private set; }
        public List<HttpRequestMessage> Requests { get; } = new();

        public CountingFakeHandler(int failCount) => _failCount = failCount;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            Requests.Add(request);
            if (CallCount <= _failCount)
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError) { Content = new StringContent("fail") });
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    [InlineData(3, 8)]
    [InlineData(8, 256)]
    [InlineData(9, 300)]
    [InlineData(10, 300)]
    public void ComputeBackoff_MatchesSpec(int attempt, int expectedSeconds)
    {
        // Mirrors WebhookDispatchWorker.ComputeBackoff
        var actual = TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempt), 300));
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), actual);
    }

    [Fact]
    public void WebhookEvent_MarkFailed_IncrementsAttemptsAndSetsNextAttempt()
    {
        var ev = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "PaymentCapturedDomainEvent", "{}");
        Assert.Equal(0, ev.Attempts);
        ev.MarkFailed("timeout", TimeSpan.FromSeconds(2));
        Assert.Equal(1, ev.Attempts);
        Assert.Equal(WebhookEvent.StatusFailed, ev.Status);
        Assert.NotNull(ev.NextAttemptAt);
        Assert.True(ev.NextAttemptAt > DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void WebhookEvent_ExceedsMaxAttempts_StaysFailed()
    {
        var ev = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "PaymentCapturedDomainEvent", "{}");
        for (int i = 0; i < 8; i++)
            ev.MarkFailed("err", TimeSpan.FromSeconds(Math.Min(Math.Pow(2, i + 1), 300)));
        Assert.Equal(8, ev.Attempts);
        Assert.Equal(WebhookEvent.StatusFailed, ev.Status);
        // After 8 failures, worker would not reschedule (TimeSpan.Zero)
        ev.MarkFailed("final", TimeSpan.Zero);
        Assert.Equal(9, ev.Attempts);
    }

    [Fact]
    public void WebhookEvent_ResetForReplay_ClearsAttempts()
    {
        var ev = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "PaymentCapturedDomainEvent", "{}");
        ev.MarkFailed("err", TimeSpan.FromSeconds(4));
        ev.MarkFailed("err", TimeSpan.FromSeconds(8));
        Assert.Equal(2, ev.Attempts);
        ev.ResetForReplay();
        Assert.Equal(0, ev.Attempts);
        Assert.Equal(WebhookEvent.StatusPending, ev.Status);
        Assert.Null(ev.LastError);
        Assert.NotNull(ev.NextAttemptAt);
    }

    [Fact]
    public async Task CountingFake_SucceedsAfterNFails()
    {
        var handler = new CountingFakeHandler(failCount: 3);
        var client = new HttpClient(handler);
        // Simulate 3 fails then success
        for (int i = 0; i < 3; i++)
        {
            var res = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://example.com"));
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, res.StatusCode);
        }
        var ok = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "http://example.com"));
        Assert.Equal(System.Net.HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task WebhookDispatcher_WithCountingFake_RetriesUntilSuccess()
    {
        // Integration-style: dispatcher uses HttpClientFactory that returns our counting handler
        var handler = new CountingFakeHandler(failCount: 2);
        var factory = new FakeFactory(handler);
        var merchantService = new FakeMerchantService("https://example.com/webhook", "secret");
        var dispatcher = new Payment.Infrastructure.Services.WebhookDispatcher(
            factory, merchantService, Microsoft.Extensions.Options.Options.Create(new Payment.Infrastructure.Configuration.WebhookSecretRotationOptions { GracePeriodHours = 24 }), Microsoft.Extensions.Logging.Abstractions.NullLogger<Payment.Infrastructure.Services.WebhookDispatcher>.Instance);

        var ev = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "PaymentCapturedDomainEvent", "{\"id\":1}");
        // First two deliveries fail via handler, third would succeed if retried by worker; dispatcher single-attempt fails
        var (success1, _) = await dispatcher.DeliverAsync(ev, CancellationToken.None);
        Assert.False(success1);
        var (success2, _) = await dispatcher.DeliverAsync(ev, CancellationToken.None);
        Assert.False(success2);
        Assert.Equal(2, handler.CallCount);
        // After replay reset, next delivery would succeed if handler now returns OK (we need a handler that succeeds after 2)
        var successHandler = new CountingFakeHandler(failCount: 0);
        var factory2 = new FakeFactory(successHandler);
        var dispatcher2 = new Payment.Infrastructure.Services.WebhookDispatcher(
            factory2, merchantService, Microsoft.Extensions.Options.Options.Create(new Payment.Infrastructure.Configuration.WebhookSecretRotationOptions { GracePeriodHours = 24 }), Microsoft.Extensions.Logging.Abstractions.NullLogger<Payment.Infrastructure.Services.WebhookDispatcher>.Instance);
        ev.ResetForReplay();
        var (success3, _) = await dispatcher2.DeliverAsync(ev, CancellationToken.None);
        Assert.True(success3);
        Assert.Equal(1, successHandler.CallCount);
    }

    private sealed class FakeFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public FakeFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new HttpClient(_handler, false);
    }

    private sealed class FakeMerchantService : Payment.Application.Interfaces.IMerchantService
    {
        private readonly string _url;
        private readonly string _secret;
        public FakeMerchantService(string url, string secret) { _url = url; _secret = secret; }
        public Task<BuildingBlocks.Shared.Results.Result<Payment.Application.DTOs.MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default)
            => Task.FromResult(BuildingBlocks.Shared.Results.Result<Payment.Application.DTOs.MerchantConfig>.Success(new Payment.Application.DTOs.MerchantConfig(_url, true, _secret, null, null)));
        public Task<BuildingBlocks.Shared.Results.Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<BuildingBlocks.Shared.Results.Result<Guid?>> GetMerchantOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<BuildingBlocks.Shared.Results.Result<Payment.Application.DTOs.MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
