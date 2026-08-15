using BuildingBlocks.Shared.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Shared.Tests;

public class JwtBearerExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var data = settings.ToDictionary(s => s.Key, s => (string?)s.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static JwtBearerOptions GetOptions(IConfiguration config)
    {
        var services = new ServiceCollection();
        services.AddPaymentSwitchJwtBearer(config);
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    private const string CurrentSecret = "current-secret-key-at-least-32-chars-long!!";
    private const string PreviousSecret = "previous-secret-key-at-least-32-chars-lon!";

    [Fact]
    public void AddPaymentSwitchJwtBearer_WithOnlyCurrentSecret_SetsSingleSigningKey()
    {
        var config = BuildConfig(
            ("Jwt:Secret", CurrentSecret),
            ("Jwt:Issuer", "IdentityService"),
            ("Jwt:Audience", "PaymentSwitch"));

        var options = GetOptions(config);

        var keys = options.TokenValidationParameters.IssuerSigningKeys.ToList();
        Assert.Single(keys);
    }

    [Fact]
    public void AddPaymentSwitchJwtBearer_WithPreviousSecret_AcceptsBothKeys()
    {
        var config = BuildConfig(
            ("Jwt:Secret", CurrentSecret),
            ("Jwt:PreviousSecret", PreviousSecret),
            ("Jwt:Issuer", "IdentityService"),
            ("Jwt:Audience", "PaymentSwitch"));

        var options = GetOptions(config);

        var keys = options.TokenValidationParameters.IssuerSigningKeys.ToList();
        Assert.Equal(2, keys.Count);
        Assert.True(options.TokenValidationParameters.ValidateIssuerSigningKey);
    }

    [Fact]
    public void AddPaymentSwitchJwtBearer_WithPreviousSecretEqualToCurrent_SetsSingleKey()
    {
        var config = BuildConfig(
            ("Jwt:Secret", CurrentSecret),
            ("Jwt:PreviousSecret", CurrentSecret));

        var options = GetOptions(config);

        Assert.Single(options.TokenValidationParameters.IssuerSigningKeys);
    }

    [Fact]
    public void AddPaymentSwitchJwtBearer_WithMissingSecret_Throws()
    {
        var config = BuildConfig(("Jwt:Issuer", "IdentityService"));

        var ex = Assert.Throws<InvalidOperationException>(() => GetOptions(config));

        Assert.Contains("Jwt:Secret", ex.Message);
    }

    [Fact]
    public void AddPaymentSwitchJwtBearer_SetsIssuerAndAudience()
    {
        var config = BuildConfig(
            ("Jwt:Secret", CurrentSecret),
            ("Jwt:Issuer", "IdentityService"),
            ("Jwt:Audience", "PaymentSwitch"));

        var options = GetOptions(config);

        Assert.Equal("IdentityService", options.TokenValidationParameters.ValidIssuer);
        Assert.Equal("PaymentSwitch", options.TokenValidationParameters.ValidAudience);
        Assert.True(options.TokenValidationParameters.ValidateIssuer);
        Assert.True(options.TokenValidationParameters.ValidateAudience);
        Assert.True(options.TokenValidationParameters.ValidateLifetime);
    }

    [Fact]
    public void AddPaymentSwitchJwtBearer_WithConfigureCallback_AppliesAdditionalOptions()
    {
        var config = BuildConfig(
            ("Jwt:Secret", CurrentSecret),
            ("Jwt:Issuer", "IdentityService"),
            ("Jwt:Audience", "PaymentSwitch"));

        var services = new ServiceCollection();
        services.AddPaymentSwitchJwtBearer(config, options =>
        {
            options.Events = new JwtBearerEvents();
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.NotNull(options.Events);
    }
}
