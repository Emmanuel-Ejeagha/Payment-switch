using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Shared.Http;

public static class CorsExtensions
{
    public const string AllowFrontendPolicy = "AllowFrontend";

    private static readonly string[] DefaultDevOrigins =
    {
        "http://localhost:5173",
        "http://localhost:3000",
        "http://localhost:3001",
        "http://localhost:3002"
    };

    /// <summary>
    /// Registers the "AllowFrontend" CORS policy from the <c>Cors:AllowedOrigins</c>
    /// config section (comma- or array-separated). Falls back to the known localhost
    /// dev origins when the section is absent so local development keeps working.
    /// Production refuses to start without explicit HTTPS origins: an absent
    /// section (or a plaintext-http origin) throws instead of silently opening
    /// the API to the wrong audience.
    /// </summary>
    public static IServiceCollection AddPaymentSwitchCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = ResolveOrigins(configuration);

        if (string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (origins is null)
            {
                throw new InvalidOperationException(
                    "Cors:AllowedOrigins (Cors__AllowedOrigins) must be set in Production. " +
                    "Configure the real frontend origin(s); there is no safe default.");
            }

            var insecure = origins
                .Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (insecure.Length > 0)
            {
                throw new InvalidOperationException(
                    "Production CORS origins must use https. " +
                    $"Insecure origin(s) rejected: {string.Join(", ", insecure)}.");
            }
        }

        origins ??= DefaultDevOrigins;

        return services.AddCors(options =>
        {
            options.AddPolicy(AllowFrontendPolicy, policy =>
            {
                policy.WithOrigins(origins)
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
    }

    private static string[]? ResolveOrigins(IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (origins is { Length: > 0 })
        {
            return origins;
        }

        var raw = configuration["Cors:AllowedOrigins"];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return null;
    }
}