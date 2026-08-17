using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BuildingBlocks.Shared.Auth;

public static class JwtBearerExtensions
{
    /// <summary>
    /// Configures JWT bearer authentication with an optional "previous" secret
    /// for zero-downtime key rotation.
    ///
    /// Rotation procedure (dual-write window):
    ///   1. Deploy with BOTH Jwt:Secret (new key) and Jwt:PreviousSecret (old
    ///      key) set. Both signing keys are accepted so already-issued tokens
    ///      keep validating while new tokens are issued with the new key.
    ///   2. After the token lifetime (or after all services have restarted),
    ///      remove Jwt:PreviousSecret and redeploy so only the new key is
    ///      accepted.
    /// </summary>
    public static IServiceCollection AddPaymentSwitchJwtBearer(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<JwtBearerOptions>? configure = null)
    {
        var jwt = configuration.GetSection("Jwt");
        var secret = jwt["Secret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set Jwt:Secret via environment or secrets.");
        }

        var signingKeys = new List<SymmetricSecurityKey>
        {
            new(Encoding.UTF8.GetBytes(secret))
        };

        var previousSecret = jwt["PreviousSecret"];
        if (!string.IsNullOrWhiteSpace(previousSecret) &&
            !string.Equals(previousSecret, secret, StringComparison.Ordinal))
        {
            signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(previousSecret)));
        }

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                    ValidIssuer = jwt["Issuer"],
                    ValidAudience = jwt["Audience"],
                    IssuerSigningKeys = signingKeys
                };

                configure?.Invoke(options);
            }).Services;
    }
}
