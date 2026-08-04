namespace PaymentSwitch.IntegrationTests.Shared;

/// <summary>
/// Supplies configuration secrets to API test hosts.
/// </summary>
/// <remarks>
/// Each API validates its security configuration via
/// <c>builder.Configuration.ValidateSecuritySecrets(...)</c> immediately after
/// <c>WebApplication.CreateBuilder(args)</c>. That runs while the host is being
/// constructed, which is *before* <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// applies any <c>ConfigureAppConfiguration</c> callback. Supplying secrets through
/// an in-memory collection is therefore too late and the host throws.
///
/// Environment variables are read by the default configuration builder inside
/// <c>CreateBuilder</c>, so they are visible to the validator. Note the double
/// underscore is the .NET convention for nesting (<c>Jwt__Secret</c> maps to <c>Jwt:Secret</c>).
/// </remarks>
public static class TestSecrets
{
    /// <summary>
    /// Password for the Postgres test container. Deliberately not the literal
    /// "paymentswitch", which <c>ValidateSecuritySecrets</c> rejects as a default credential.
    /// </summary>
    public const string PostgresPassword = "integration-test-db-password";

    public const string JwtSecret = "integration-test-jwt-secret-32-bytes-min!";
    public const string JwtAudience = "PaymentSwitch";
    public const string RabbitMqUserName = "integration-test";
    public const string RabbitMqPassword = "integration-test-rabbit-password";

    /// <summary>
    /// Host used for the RabbitMQ readiness probe during tests.
    /// </summary>
    /// <remarks>
    /// The readiness probe opens a raw TCP connection to this host on port 5672.
    /// Pointing it at "localhost" makes the outcome depend on whether the developer
    /// happens to have the compose stack running, so <c>Ready_Returns503_WhenRabbitMqUnavailable</c>
    /// passes on CI and fails locally. ".invalid" is reserved by RFC 2606 and never
    /// resolves, so the probe fails fast and the premise of the test actually holds.
    /// </remarks>
    public const string RabbitMqHostName = "rabbitmq.invalid";

    private static readonly object Sync = new();

    /// <summary>
    /// Sets the security configuration the API validates at startup.
    /// Call from the factory constructor, before the host is built.
    /// </summary>
    public static void ApplyEnvironment(string jwtIssuer)
    {
        lock (Sync)
        {
            Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
            Environment.SetEnvironmentVariable("Jwt__Issuer", jwtIssuer);
            Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
            Environment.SetEnvironmentVariable("RabbitMQ__HostName", RabbitMqHostName);
            Environment.SetEnvironmentVariable("RabbitMQ__UserName", RabbitMqUserName);
            Environment.SetEnvironmentVariable("RabbitMQ__Password", RabbitMqPassword);
        }
    }

    /// <summary>
    /// Publishes the test container connection string once its port is known.
    /// The container only gets a host port after it starts, so this runs after
    /// <c>StartAsync</c> but before the host is first resolved.
    /// </summary>
    public static void ApplyConnectionString(string databaseName, string connectionString)
    {
        Environment.SetEnvironmentVariable(
            $"ConnectionStrings__{databaseName}", connectionString);
    }
}
