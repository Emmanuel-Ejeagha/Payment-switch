using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Configuration;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace Payment.Infrastructure.Services;

public class WebhookDispatcher
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMerchantService _merchantService;
    private readonly IOptions<WebhookSecretRotationOptions> _rotationOptions;
    private readonly ILogger<WebhookDispatcher> _logger;

    public WebhookDispatcher(
        IHttpClientFactory httpClientFactory,
        IMerchantService merchantService,
        IOptions<WebhookSecretRotationOptions> rotationOptions,
        ILogger<WebhookDispatcher> logger)
    {
        _httpClientFactory = httpClientFactory;
        _merchantService = merchantService;
        _rotationOptions = rotationOptions;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> DeliverAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        var configResult = await _merchantService.GetMerchantConfigAsync(webhookEvent.MerchantId, cancellationToken);
        if (!configResult.IsSuccess)
            return (false, "Unable to retrieve merchant configuration.");

        var config = configResult.Value!;
        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
            return (false, "No webhook endpoint configured.");

        var payloadBytes = Encoding.UTF8.GetBytes(webhookEvent.Payload);
        var signingSecret = WebhookSecretResolver.SelectSigningSecret(
            config.WebhookSecret,
            config.PreviousWebhookSecret,
            config.WebhookSecretRotatedAtUtc,
            DateTime.UtcNow,
            TimeSpan.FromHours(_rotationOptions.Value.GracePeriodHours));
        var signature = WebhookSignature.Compute(signingSecret, payloadBytes, out var timestamp);

        using var client = _httpClientFactory.CreateClient("webhook");
        using var request = new HttpRequestMessage(HttpMethod.Post, config.WebhookUrl);
        request.Content = new ByteArrayContent(payloadBytes);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Headers.Add("X-PaymentSwitch-Signature", signature);
        request.Headers.Add("X-PaymentSwitch-Timestamp", timestamp);
        request.Headers.Add("X-PaymentSwitch-Event", webhookEvent.EventType);
        request.Headers.Add("X-PaymentSwitch-Delivery", webhookEvent.Id.ToString());

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
                return (true, null);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var snippet = body.Length > 500 ? body[..500] : body;
            _logger.LogWarning("Webhook {EventId} to {Url} returned {(int)StatusCode} {StatusCode}: {Body}",
                webhookEvent.Id, config.WebhookUrl, (int)response.StatusCode, response.StatusCode, snippet);
            return (false, $"Endpoint returned {(int)response.StatusCode} {response.StatusCode}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook {EventId} to {Url} failed", webhookEvent.Id, config.WebhookUrl);
            return (false, ex.Message);
        }
    }
}

public static class WebhookSignature
{
    public static string Compute(string? secret, byte[] payload, out string timestamp)
    {
        timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var keyBytes = Encoding.UTF8.GetBytes(secret ?? string.Empty);
        var body = Encoding.UTF8.GetBytes($"{timestamp}.{Convert.ToBase64String(payload)}");
        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(body);
        return $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    /// <summary>
    /// Constant-time verification of a received webhook signature. Used by
    /// merchants to validate a delivery (see docs/webhooks.md). The signature
    /// is an HMAC-SHA256 of "<timestamp>.<base64(payload)>" with the signing
    /// secret, formatted "sha256=&lt;hex&gt;".
    /// </summary>
    public static bool Verify(string? secret, byte[] payload, string timestamp, string signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
            return false;

        var expected = Compute(secret, payload, out _);
        if (expected is null || !expected.StartsWith("sha256=", StringComparison.Ordinal))
            return false;

        var expectedHex = expected[7..];
        var providedHex = signature.Length == expected.Length && signature.StartsWith("sha256=", StringComparison.Ordinal)
            ? signature[7..]
            : null;
        if (providedHex is null)
            return false;

        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(expectedHex),
                Convert.FromHexString(providedHex));
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

/// <summary>
/// Selects which webhook secret to sign with. After a rotation the merchant's
/// verifier still holds the previous secret, so webhooks keep being signed with
/// it during the grace window and only switch to the new one afterwards
/// (TASK-006). If the secret was never rotated the current secret is used.
/// </summary>
public static class WebhookSecretResolver
{
    public static string? SelectSigningSecret(
        string? current,
        string? previous,
        DateTime? rotatedAtUtc,
        DateTime nowUtc,
        TimeSpan gracePeriod)
    {
        if (string.IsNullOrWhiteSpace(previous) || rotatedAtUtc is null)
            return current;

        return nowUtc - rotatedAtUtc.Value < gracePeriod ? previous : current;
    }
}
