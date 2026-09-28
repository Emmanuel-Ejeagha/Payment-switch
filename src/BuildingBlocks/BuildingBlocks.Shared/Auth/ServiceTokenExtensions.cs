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
}
