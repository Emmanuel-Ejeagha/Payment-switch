using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Shared.Configuration;

public static class SecretValidationExtensions
{
    private static readonly HashSet<string> KnownInsecureSecrets = new(StringComparer.OrdinalIgnoreCase)
    {
        "your-super-secret-key-minimum-32-bytes!",
        "your-super-secret-key-minimum-32-bytes",
        "change-me-to-a-real-secret-at-least-32-chars!"
    };

    public static void ValidateSecuritySecrets(
        this IConfiguration configuration,
        string databaseName)
    {
        ValidateJwtSecret(configuration["Jwt:Secret"]);        var connectionString = configuration.GetConnectionString(databaseName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{databaseName}' is not configured. Set ConnectionStrings:{databaseName}.");
        }

        if (connectionString.Contains("Password=paymentswitch", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection string '{databaseName}' uses the default 'paymentswitch' password. " +
                "Set a strong password via ConnectionStrings:{databaseName}.");
        }

        if (connectionString.Contains("Password=", StringComparison.OrdinalIgnoreCase) &&
            connectionString.Contains("Password=;", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Connection string '{databaseName}' has an empty password. " +
                "Set a strong password via ConnectionStrings:{databaseName}.");
        }

        ValidateRabbitMqCredentials(
            configuration["RabbitMQ:UserName"],
            configuration["RabbitMQ:Password"]);
    }

    /// <summary>
    /// Validates the AES-GCM key used to encrypt merchant webhook secrets at
    /// rest (TASK-006). Only the Merchant service calls this.
    /// </summary>
    public static void ValidateWebhookSecretEncryptionKey(this IConfiguration configuration)
    {
        var key = configuration["WebhookSecretEncryption:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "WebhookSecretEncryption:Key is not configured. Set it via environment or secrets.");
        }

        if (key.Length < 32)
        {
            throw new InvalidOperationException(
                $"WebhookSecretEncryption:Key must be at least 32 characters (got {key.Length}).");
        }

        if (KnownInsecureSecrets.Contains(key))
        {
            throw new InvalidOperationException(
                "WebhookSecretEncryption:Key is set to a known insecure placeholder. Set a unique, strong secret.");
        }
    }

    private static void ValidateJwtSecret(string? secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set Jwt:Secret via environment or secrets.");
        }

        if (secret.Length < 32)
        {
            throw new InvalidOperationException(
                $"Jwt:Secret must be at least 32 characters (got {secret.Length}).");
        }

        if (KnownInsecureSecrets.Contains(secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is set to a known insecure placeholder. Set a unique, strong secret.");
        }
    }

    private static void ValidateRabbitMqCredentials(string? userName, string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || string.Equals(password, "guest", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "RabbitMQ:Password must not be empty or the default 'guest'. Set a strong RabbitMQ:Password.");
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new InvalidOperationException(
                "RabbitMQ:UserName must not be empty. Set a RabbitMQ:UserName.");
        }
    }
}
