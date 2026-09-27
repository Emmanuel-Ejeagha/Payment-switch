using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.API.Middlewares;

public class SecretKeyAuthMiddleware
{
    private const string PublicPaymentsPrefix = "/v1/payments";
    private const string LivePrefix = "sk_live_";
    private const string TestPrefix = "sk_test_";

    private readonly RequestDelegate _next;
    private readonly ILogger<SecretKeyAuthMiddleware> _logger;
    private readonly ApiKeyResolutionCache _cache;
    private readonly ApiKeyFailureThrottle _throttle;

    public SecretKeyAuthMiddleware(
        RequestDelegate next,
        ILogger<SecretKeyAuthMiddleware> logger,
        ApiKeyResolutionCache cache,
        ApiKeyFailureThrottle throttle)
    {
        _next = next;
        _logger = logger;
        _cache = cache;
        _throttle = throttle;
    }

    public async Task InvokeAsync(HttpContext context, IMerchantService merchantService)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (!path.StartsWith(PublicPaymentsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Per-IP partition (API-key callers are unauthenticated at this point;
        // ForwardedHeaders already restored the real client IP).
        var partitionKey = $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";
        if (_throttle.IsThrottled(partitionKey))
        {
            await WriteTooManyRequests(context);
            return;
        }

        var apiKey = ExtractApiKey(context.Request);
        if (string.IsNullOrEmpty(apiKey) || !TryParseKey(apiKey, out var keyPrefix))
        {
            _throttle.RecordFailure(partitionKey);
            await WriteUnauthorized(context, "Missing or malformed API key. Expected 'Authorization: Bearer sk_live_...'.");
            return;
        }

        var cacheKey = ApiKeyResolutionCache.BuildCacheKey(keyPrefix, apiKey);
        if (!_cache.TryGet(cacheKey, out MerchantKeyResolution? resolution))
        {
            var result = await merchantService.ResolveApiKeyAsync(keyPrefix, apiKey, context.RequestAborted);
            if (!result.IsSuccess)
            {
                _throttle.RecordFailure(partitionKey);
                await WriteUnauthorized(context, "Invalid API key.");
                return;
            }
            resolution = result.Value;
            _cache.Set(cacheKey, resolution!);
        }

        if (resolution is null)
        {
            _throttle.RecordFailure(partitionKey);
            await WriteUnauthorized(context, "Invalid API key.");
            return;
        }

        if (!string.Equals(resolution.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            await WriteForbidden(context, "Merchant is not active.");
            return;
        }

        context.Items["MerchantId"] = resolution.MerchantId;
        context.Items["MerchantEnvironment"] = resolution.Environment;
        await _next(context);
    }

    private static string? ExtractApiKey(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var value = authHeader.ToString();
            if (value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return value["Bearer ".Length..].Trim();
        }
        if (request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
            return apiKeyHeader.ToString().Trim();
        return null;
    }

    private static bool TryParseKey(string apiKey, out string keyPrefix)
    {
        keyPrefix = string.Empty;
        if (apiKey.StartsWith(LivePrefix, StringComparison.Ordinal))
        {
            keyPrefix = LivePrefix;
            return true;
        }
        if (apiKey.StartsWith(TestPrefix, StringComparison.Ordinal))
        {
            keyPrefix = TestPrefix;
            return true;
        }
        return false;
    }

    private static Task WriteUnauthorized(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = detail
        };
        return context.Response.WriteAsJsonAsync(problem);
    }

    private static Task WriteForbidden(HttpContext context, string detail)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Detail = detail
        };
        return context.Response.WriteAsJsonAsync(problem);
    }

    private static Task WriteTooManyRequests(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Detail = "Too many failed API-key attempts. Retry after the window resets."
        };
        return context.Response.WriteAsJsonAsync(problem);
    }
}
