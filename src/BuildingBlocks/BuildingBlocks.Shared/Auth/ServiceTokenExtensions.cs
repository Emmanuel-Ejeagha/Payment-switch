using Grpc.Core;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
namespace BuildingBlocks.Shared.Auth;

public static class ServiceTokenExtensions
{
    public static IServiceCollection AddServiceTokenProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var section = configuration.GetSection(ServiceTokenOptions.SectionName);
        var jwt = configuration.GetSection("Jwt");

        // Step 2.9: the service-token secret must be dedicated — falling back
        // to the user JWT signing key would let a leaked service token mint
        // user sessions and vice versa. Fail fast with an actionable message.
        var secret = section["Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Service token secret is not configured. Set ServiceToken:Secret to a dedicated value (it must differ from Jwt:Secret).");
        var jwtSecret = jwt["Secret"];
        if (!string.IsNullOrWhiteSpace(jwtSecret)
            && string.Equals(secret, jwtSecret, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "ServiceToken:Secret must differ from Jwt:Secret. Service-to-service tokens must not share the user JWT signing key.");

        var options = new ServiceTokenOptions
        {
            ServiceName = section["ServiceName"] ?? serviceName,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 10,
            Issuer = section["Issuer"] ?? jwt["Issuer"] ?? "IdentityService",
            Audience = section["Audience"] ?? jwt["Audience"] ?? "PaymentSwitch",
            Secret = secret
        };

        services.AddSingleton(new ServiceTokenProvider(options));

        // Register the matching validation handler. Minting without validation
        // leaves every ServiceOnly endpoint rejecting service tokens (they fail
        // signature checks against the user-JWT scheme), so both halves live
        // behind this one call.
        services.AddAuthentication()
            .AddJwtBearer(ServiceTokenOptions.AuthenticationScheme, bearer =>
            {
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidIssuer = options.Issuer,
                    ValidAudience = options.Audience,
                    IssuerSigningKeys = new[]
                    {
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Secret))
                    }
                };
            });
        return services;
    }

    public static IHttpClientBuilder AddServiceTokenAuthentication(this IHttpClientBuilder builder)
    {
        return builder.AddCallCredentials((context, metadata, serviceProvider) =>
        {
            var provider = serviceProvider.GetRequiredService<ServiceTokenProvider>();
            metadata.Add("Authorization", $"Bearer {provider.GetToken()}");
            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// Validates the gRPC channel address is HTTPS in Production. In Development
    /// insecure channels are allowed for local Docker convenience.
    /// Step 7.3: an explicit in-network exception can be acknowledged via
    /// <paramref name="allowInsecureException"/> (bound to
    /// Grpc:{Service}:AllowInsecure). The exception is only valid when the
    /// channel stays on the isolated docker/k8s network (port 5001 never
    /// published), the server endpoint is ServiceOnly-gated, and callers use
    /// short-lived service tokens — see docs/tls.md "gRPC service-to-service".
    /// Prefer HTTPS with a mounted cert; use the exception only where mTLS is
    /// not yet provisioned.
    /// </summary>
    public static void RequireGrpcTls(IConfiguration configuration, string channelAddress)
        => RequireGrpcTls(configuration, channelAddress, allowInsecureException: false);

    public static void RequireGrpcTls(IConfiguration configuration, string channelAddress, bool allowInsecureException)
    {
        if (!IsProduction(configuration))
            return;

        if (allowInsecureException)
            return;

        if (!Uri.TryCreate(channelAddress, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new InvalidOperationException(
                $"gRPC channel '{channelAddress}' must use https in Production. " +
                "Set Grpc:{Service}:Address to an https:// endpoint, or explicitly acknowledge the " +
                "in-network exception with Grpc:{Service}:AllowInsecure=true (isolated network + " +
                "ServiceOnly policy only — see docs/tls.md).");
    }

    /// <summary>
    /// True when the channel address is plain http and must use insecure call
    /// credentials. HTTPS channels must NOT set
    /// UnsafeUseInsecureChannelCallCredentials (Step 7.3 fix: the flag was
    /// previously set unconditionally, weakening TLS even when configured).
    /// </summary>
    public static bool ShouldUseInsecureChannel(string channelAddress) =>
        Uri.TryCreate(channelAddress, UriKind.Absolute, out var uri) &&
        string.Equals(uri.Scheme, "http", StringComparison.OrdinalIgnoreCase);

    private static bool IsProduction(IConfiguration configuration)
    {
        var env = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        return string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase);
    }
}
