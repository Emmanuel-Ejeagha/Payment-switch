using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Tests.Services;

public class WebhookSecretResolverTests
{
    private static readonly DateTime Now = new(2026, 8, 11, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Grace = TimeSpan.FromHours(72);

    [Fact]
    public void SelectSigningSecret_NeverRotated_UsesCurrent()
    {
        var result = WebhookSecretResolver.SelectSigningSecret("current", null, null, Now, Grace);

        Assert.Equal("current", result);
    }

    [Fact]
    public void SelectSigningSecret_WithinGrace_UsesPrevious()
    {
        var rotatedAt = Now.AddHours(-24);

        var result = WebhookSecretResolver.SelectSigningSecret("current", "previous", rotatedAt, Now, Grace);

        Assert.Equal("previous", result);
    }

    [Fact]
    public void SelectSigningSecret_AfterGrace_UsesCurrent()
    {
        var rotatedAt = Now.AddHours(-120);

        var result = WebhookSecretResolver.SelectSigningSecret("current", "previous", rotatedAt, Now, Grace);

        Assert.Equal("current", result);
    }

    [Fact]
    public void SelectSigningSecret_JustInsideGraceBoundary_UsesPrevious()
    {
        var rotatedAt = Now.AddHours(-71);

        var result = WebhookSecretResolver.SelectSigningSecret("current", "previous", rotatedAt, Now, Grace);

        Assert.Equal("previous", result);
    }

    [Fact]
    public void SelectSigningSecret_AtGraceBoundary_UsesCurrent()
    {
        var rotatedAt = Now.AddHours(-72);

        var result = WebhookSecretResolver.SelectSigningSecret("current", "previous", rotatedAt, Now, Grace);

        Assert.Equal("current", result);
    }

    [Fact]
    public void OldSecretStillVerifiesWithinGrace()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":42}");
        var rotatedAt = Now.AddHours(-24);
        var signingSecret = WebhookSecretResolver.SelectSigningSecret("current", "old-secret", rotatedAt, Now, Grace);

        var oldSignature = WebhookSignature.Compute("old-secret", payload, out var oldTimestamp);
        var actualSignature = WebhookSignature.Compute(signingSecret, payload, out _);

        Assert.Equal(oldSignature, actualSignature);
        Assert.False(string.IsNullOrEmpty(oldTimestamp));
    }
}
