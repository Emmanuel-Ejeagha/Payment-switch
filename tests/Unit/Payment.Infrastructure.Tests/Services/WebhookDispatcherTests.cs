using Payment.Domain.Entities;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Tests.Services;

public class WebhookSignatureTests
{
    [Fact]
    public void Compute_ShouldReturnSha256PrefixedSignature()
    {
        var signature = WebhookSignature.Compute("secret", System.Text.Encoding.UTF8.GetBytes("{\"id\":1}"), out var timestamp);

        Assert.StartsWith("sha256=", signature);
        Assert.Equal(7 + 64, signature.Length);
        Assert.False(string.IsNullOrEmpty(timestamp));
    }

    [Fact]
    public void Compute_SamePayloadDifferentSecrets_ShouldDiffer()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("payload");
        var sig1 = WebhookSignature.Compute("secret-a", payload, out _);
        var sig2 = WebhookSignature.Compute("secret-b", payload, out _);

        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void Compute_EmptySecret_ShouldNotThrow()
    {
        var signature = WebhookSignature.Compute(null, System.Text.Encoding.UTF8.GetBytes("payload"), out _);

        Assert.StartsWith("sha256=", signature);
    }
}

public class WebhookEventTests
{
    [Fact]
    public void Constructor_ShouldBePendingAndDueNow()
    {
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "PaymentAuthorizedDomainEvent", "{}");

        Assert.Equal(WebhookEvent.StatusPending, webhookEvent.Status);
        Assert.Equal(0, webhookEvent.Attempts);
        Assert.NotNull(webhookEvent.NextAttemptAt);
        Assert.True(webhookEvent.NextAttemptAt <= DateTime.UtcNow);
    }

    [Fact]
    public void MarkSucceeded_ShouldClearRetry()
    {
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "t", "{}");

        webhookEvent.MarkSucceeded();

        Assert.Equal(WebhookEvent.StatusSucceeded, webhookEvent.Status);
        Assert.Null(webhookEvent.NextAttemptAt);
        Assert.Null(webhookEvent.LastError);
    }

    [Fact]
    public void MarkFailed_ShouldIncrementAttemptsAndScheduleRetry()
    {
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "t", "{}");

        webhookEvent.MarkFailed("timeout", TimeSpan.FromSeconds(30));

        Assert.Equal(WebhookEvent.StatusFailed, webhookEvent.Status);
        Assert.Equal(1, webhookEvent.Attempts);
        Assert.Equal("timeout", webhookEvent.LastError);
        Assert.True(webhookEvent.NextAttemptAt > DateTime.UtcNow);
    }

    [Fact]
    public void ResetForReplay_ShouldReturnToPending()
    {
        var webhookEvent = new WebhookEvent(Guid.NewGuid(), Guid.NewGuid(), "t", "{}");
        webhookEvent.MarkFailed("boom", TimeSpan.FromSeconds(30));

        webhookEvent.ResetForReplay();

        Assert.Equal(WebhookEvent.StatusPending, webhookEvent.Status);
        Assert.Equal(0, webhookEvent.Attempts);
        Assert.Null(webhookEvent.LastError);
    }
}
