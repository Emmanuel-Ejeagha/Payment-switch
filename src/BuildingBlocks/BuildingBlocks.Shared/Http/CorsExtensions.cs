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
    /// Production sets <c>Cors__AllowedOrigins</c> to the real HTTPS origin only.
    /// </summary>
    public static IServiceCollection AddPaymentSwitchCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = ResolveOrigins(configuration);
        var env = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");

        if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
        {
            if (origins.Length == 0 || origins.SequenceEqual(DefaultDevOrigins))
                throw new InvalidOperationException("Cors:AllowedOrigins must be explicitly configured in Production (no localhost fallback).");
            foreach (var origin in origins)
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http"))
                    throw new InvalidOperationException($"Cors origin '{origin}' must be an absolute URL.");
                if (uri.Scheme != "https")
                    throw new InvalidOperationException($"Cors origin '{origin}' must use https in Production.");
            }
        }

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

    private static string[] ResolveOrigins(IConfiguration configuration)
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

        return DefaultDevOrigins;
    }
}