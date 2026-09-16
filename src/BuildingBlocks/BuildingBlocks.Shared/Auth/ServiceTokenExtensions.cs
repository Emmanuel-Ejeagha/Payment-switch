using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        var options = new ServiceTokenOptions
        {
            ServiceName = section["ServiceName"] ?? serviceName,
            ExpiryMinutes = int.TryParse(section["ExpiryMinutes"], out var minutes) ? minutes : 10,
            Issuer = section["Issuer"] ?? jwt["Issuer"] ?? "IdentityService",
            Audience = section["Audience"] ?? jwt["Audience"] ?? "PaymentSwitch",
            Secret = section["Secret"] ?? jwt["Secret"]
                ?? throw new InvalidOperationException(
                    "Service token secret is not configured. Set ServiceToken:Secret or Jwt:Secret.")
        };

        services.AddSingleton(new ServiceTokenProvider(options));
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
