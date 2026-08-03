using BuildingBlocks.Shared.Configuration;
using Microsoft.Extensions.Configuration;

namespace BuildingBlocks.Shared.Tests;

public class SecretValidationExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var data = settings.ToDictionary(s => s.Key, s => (string?)s.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static readonly (string, string)[] ValidRabbitMq =
    {
        ("RabbitMQ:UserName", "paymentswitch"),
        ("RabbitMQ:Password", "a-strong-rabbitmq-password")
    };

    [Fact]
    public void Validate_WithMissingJwtSecret_Throws()
    {
        var config = BuildConfig(ValidRabbitMq);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateSecuritySecrets("IdentityDb"));

        Assert.Contains("Jwt:Secret", ex.Message);
    }

    [Fact]
    public void Validate_WithShortJwtSecret_Throws()
    {
        var config = BuildConfig(new[]
        {
            ("Jwt:Secret", "short"),
            ("ConnectionStrings:IdentityDb", "Host=localhost;Password=strong-password;")
        }.Concat(ValidRabbitMq).ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateSecuritySecrets("IdentityDb"));

        Assert.Contains("32 characters", ex.Message);
    }

    [Fact]
    public void Validate_WithInsecurePlaceholderJwtSecret_Throws()
    {
        var config = BuildConfig(new[]
        {
            ("Jwt:Secret", "your-super-secret-key-minimum-32-bytes!"),
            ("ConnectionStrings:IdentityDb", "Host=localhost;Password=strong-password;")
        }.Concat(ValidRabbitMq).ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateSecuritySecrets("IdentityDb"));

        Assert.Contains("known insecure placeholder", ex.Message);
    }

    [Fact]
    public void Validate_WithDefaultDbPassword_Throws()
    {
        var config = BuildConfig(new[]
        {
            ("Jwt:Secret", "a-strong-32-byte-secret-key-for-testing!!"),
            ("ConnectionStrings:IdentityDb", "Host=localhost;Username=paymentswitch;Password=paymentswitch")
        }.Concat(ValidRabbitMq).ToArray());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateSecuritySecrets("IdentityDb"));

        Assert.Contains("default 'paymentswitch' password", ex.Message);
    }

    [Fact]
    public void Validate_WithGuestRabbitMqCredentials_Throws()
    {
        var config = BuildConfig(new[]
        {
            ("Jwt:Secret", "a-strong-32-byte-secret-key-for-testing!!"),
            ("ConnectionStrings:IdentityDb", "Host=localhost;Password=strong-password;"),
            ("RabbitMQ:UserName", "guest"),
            ("RabbitMQ:Password", "guest")
        });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            config.ValidateSecuritySecrets("IdentityDb"));

        Assert.Contains("RabbitMQ", ex.Message);
    }

    [Fact]
    public void Validate_WithValidConfiguration_DoesNotThrow()
    {
        var config = BuildConfig(new[]
        {
            ("Jwt:Secret", "a-strong-32-byte-secret-key-for-testing!!"),
            ("ConnectionStrings:IdentityDb", "Host=localhost;Username=paymentswitch;Password=strong-db-password")
        }.Concat(ValidRabbitMq).ToArray());

        config.ValidateSecuritySecrets("IdentityDb");
    }
}
