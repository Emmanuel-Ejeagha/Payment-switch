using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Shared.Middleware;

public class RequestSizeLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly long _maxRequestBodySize;
    private readonly ILogger<RequestSizeLimitMiddleware> _logger;

    public RequestSizeLimitMiddleware(
        RequestDelegate next,
        long maxRequestBodySize,
        ILogger<RequestSizeLimitMiddleware> logger)
    {
        _next = next;
        _maxRequestBodySize = maxRequestBodySize;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.ContentLength > _maxRequestBodySize)
        {
            _logger.LogWarning(
                "Request rejected: Content-Length {ContentLength} exceeds limit {MaxSize}",
                context.Request.ContentLength,
                _maxRequestBodySize);

            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                $"{{\"error\":\"Request body too large. Maximum allowed size is {_maxRequestBodySize} bytes.\"}}");
            return;
        }

        await _next(context);
    }
}

public static class RequestSizeLimitMiddlewareExtensions
{
    private const long DefaultMaxRequestBodySize = 10 * 1024 * 1024; // 10 MB

    public static IApplicationBuilder UseRequestSizeLimit(
        this IApplicationBuilder app,
        long maxRequestBodySize = DefaultMaxRequestBodySize)
    {
        return app.UseMiddleware<RequestSizeLimitMiddleware>(maxRequestBodySize);
    }
}
