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

    [Fact]
    public void Verify_ValidSignature_ShouldPass()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("secret", payload, out var timestamp);

        Assert.True(WebhookSignature.Verify("secret", payload, timestamp, signature));
    }

    [Fact]
    public void Verify_TamperedPayload_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("secret", payload, out var timestamp);

        var tampered = System.Text.Encoding.UTF8.GetBytes("{\"id\":2}");
        Assert.False(WebhookSignature.Verify("secret", tampered, timestamp, signature));
    }

    [Fact]
    public void Verify_WrongSecret_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("payload");
        var signature = WebhookSignature.Compute("secret-a", payload, out var timestamp);

        Assert.False(WebhookSignature.Verify("secret-b", payload, timestamp, signature));
    }

    [Fact]
    public void Verify_EmptySignature_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("payload");

        Assert.False(WebhookSignature.Verify("secret", payload, "1755500000", string.Empty));
    }

    [Fact]
    public void Verify_MalformedSignature_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("payload");

        Assert.False(WebhookSignature.Verify("secret", payload, "1755500000", "sha256=zz-not-hex"));
    }

    [Fact]
    public void Verify_WrongTimestamp_ShouldFail()
    {
        // The signature binds the transmitted timestamp: verifying the same
        // signature against any other timestamp must fail. (The old code
        // ignored the supplied timestamp and re-signed with UtcNow, so this
        // passed whenever both calls landed in the same second.)
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("secret", payload, out _);

        Assert.False(WebhookSignature.Verify("secret", payload, "1000000000", signature));
    }

    [Fact]
    public void Verify_MissingTimestamp_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("payload");
        var signature = WebhookSignature.Compute("secret", payload, out _);

        Assert.False(WebhookSignature.Verify("secret", payload, null, signature));
        Assert.False(WebhookSignature.Verify("secret", payload, "  ", signature));
    }

    [Fact]
    public void VerifyWithRotation_CurrentSecret_ShouldPass()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("new-secret", payload, out var timestamp);

        Assert.True(WebhookSignature.VerifyWithRotation("new-secret", "old-secret", payload, timestamp, signature));
    }

    [Fact]
    public void VerifyWithRotation_PreviousSecret_ShouldPass()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("old-secret", payload, out var timestamp);

        Assert.True(WebhookSignature.VerifyWithRotation("new-secret", "old-secret", payload, timestamp, signature));
    }

    [Fact]
    public void VerifyWithRotation_UnknownSecret_ShouldFail()
    {
        var payload = System.Text.Encoding.UTF8.GetBytes("{\"id\":1}");
        var signature = WebhookSignature.Compute("attacker-secret", payload, out var timestamp);

        Assert.False(WebhookSignature.VerifyWithRotation("new-secret", "other-secret", payload, timestamp, signature));
        Assert.False(WebhookSignature.VerifyWithRotation("new-secret", null, payload, timestamp, signature));
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
