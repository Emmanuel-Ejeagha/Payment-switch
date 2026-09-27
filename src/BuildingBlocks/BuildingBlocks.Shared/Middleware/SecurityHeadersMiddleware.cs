using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Shared.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // OnStarting runs when the response starts — after downstream
        // middleware, so Request.IsHttps already reflects X-Forwarded-Proto
        // processed by UsePaymentSwitchForwardedHeaders (Step 8.2).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            // Aligned with the nginx edge (SAMEORIGIN): identical duplicates
            // through the proxy are benign; DENY previously disagreed.
            context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            context.Response.Headers["X-XSS-Protection"] = "0";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'self'";
            // HSTS is an edge concern first (nginx sets it), but the app emits
            // it too whenever it knows the outer scheme is https — covering
            // direct/loopback access and any future TLS-at-app deployment.
            // Plain-HTTP requests get no HSTS (a non-TLS origin must never
            // assert it).
            if (context.Request.IsHttps)
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            return Task.CompletedTask;
        });

        await _next(context);
    }
}

public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UsePaymentSwitchSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
