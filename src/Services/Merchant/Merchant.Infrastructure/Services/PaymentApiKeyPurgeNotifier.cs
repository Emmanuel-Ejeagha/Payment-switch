using BuildingBlocks.Shared.Auth;
using Merchant.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Merchant.Infrastructure.Services;

/// <summary>
/// Purge options for Payment's API-key resolution cache.
/// </summary>
public sealed class PaymentPurgeOptions
{
    public const string SectionName = "Payment";

    /// <summary>Payment API base URL (internal network). Default targets compose/k8s service DNS.</summary>
    public string BaseUrl { get; set; } = "http://payment-api:8080";
}

/// <summary>
/// Best-effort POST to Payment's ServiceOnly purge endpoint after a revoke
/// (Step 7.4). Authenticated with a short-lived service token; every failure
/// (network, non-2xx, timeout) is swallowed after a warning log because the
/// short cache TTL backstops a missed purge.
/// </summary>
public sealed class PaymentApiKeyPurgeNotifier : IApiKeyRevocationNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ServiceTokenProvider _tokenProvider;
    private readonly IOptionsMonitor<PaymentPurgeOptions> _options;
    private readonly ILogger<PaymentApiKeyPurgeNotifier> _logger;

    public PaymentApiKeyPurgeNotifier(
        HttpClient httpClient,
        ServiceTokenProvider tokenProvider,
        IOptionsMonitor<PaymentPurgeOptions> options,
        ILogger<PaymentApiKeyPurgeNotifier> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options;
        _logger = logger;
    }

    public async Task NotifyRevokedAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.CurrentValue.BaseUrl.TrimEnd('/')}/api/v1/internal/apikeys/purge");
            request.Content = JsonContent.Create(new { merchantId });
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenProvider.GetToken());

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning(
                    "API-key purge notify for merchant {MerchantId} returned {StatusCode}; cache TTL backstops",
                    merchantId, (int)response.StatusCode);
        }
        // codeql[cs/catch-of-all-exceptions]: best-effort cross-service notify;
        // the API-key cache TTL backstops a failed notification.
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "API-key purge notify for merchant {MerchantId} failed; cache TTL backstops",
                merchantId);
        }
    }
}
