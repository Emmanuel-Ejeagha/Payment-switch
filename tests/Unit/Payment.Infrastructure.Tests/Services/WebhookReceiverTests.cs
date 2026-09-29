using System.Text;
using Payment.Infrastructure.Services;

namespace Payment.Infrastructure.Tests.Services;

/// <summary>
/// Step 7.3: receiver-side webhook hardening — 5-minute freshness enforced over
/// the sender-supplied timestamp, dual-secret rotation grace, and delivery-id
/// idempotency. Includes a regression test for the fixed Verify bug (it
/// previously recomputed HMAC with a fresh timestamp, so verification only
/// passed within the same second).
/// </summary>
public class WebhookReceiverTests
{
    private static readonly TimeSpan Grace = TimeSpan.FromHours(72);
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    private static string FreshTimestamp() =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

    [Fact]
    public void Verify_UsesSuppliedTimestamp_NotFreshTimestamp()
    {
        // Regression: signature bound to timestamp T must verify with T and
        // fail with a different timestamp, regardless of "now".
        var payload = Encoding.UTF8.GetBytes("{\"id\":1}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("secret", payload, timestamp);

        Assert.True(WebhookSignature.Verify("secret", payload, timestamp, signature, Window));

        var otherTimestamp = (long.Parse(timestamp) - 60).ToString();
        Assert.False(WebhookSignature.Verify("secret", payload, otherTimestamp, signature, Window));
    }

    [Fact]
    public void ComputeWithTimestamp_IsDeterministic()
    {
        var payload = Encoding.UTF8.GetBytes("payload");
        var timestamp = FreshTimestamp();

        Assert.Equal(
            WebhookSignature.ComputeWithTimestamp("s", payload, timestamp),
            WebhookSignature.ComputeWithTimestamp("s", payload, timestamp));
    }

    [Fact]
    public void Verify_StaleTimestampSignedAtThatTime_FailsFreshness()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":1}");
        var staleTimestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600).ToString();
        var staleSignature = WebhookSignature.ComputeWithTimestamp("secret", payload, staleTimestamp);

        Assert.False(WebhookSignature.Verify("secret", payload, staleTimestamp, staleSignature, Window));
    }

    [Fact]
    public void Verify_FutureTimestampBeyondWindow_Fails()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":1}");
        var futureTimestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 600).ToString();
        var futureSignature = WebhookSignature.ComputeWithTimestamp("secret", payload, futureTimestamp);

        Assert.False(WebhookSignature.Verify("secret", payload, futureTimestamp, futureSignature, Window));
    }

    [Fact]
    public void VerifyWithRotation_CurrentSecret_Passes()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":7}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("current", payload, timestamp);

        Assert.True(WebhookSignature.VerifyWithRotation(
            "current", "previous", DateTime.UtcNow.AddHours(-1),
            payload, timestamp, signature, Grace, Window));
    }

    [Fact]
    public void VerifyWithRotation_PreviousSecretWithinGrace_Passes()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":7}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("previous", payload, timestamp);

        Assert.True(WebhookSignature.VerifyWithRotation(
            "current", "previous", DateTime.UtcNow.AddHours(-24),
            payload, timestamp, signature, Grace, Window));
    }

    [Fact]
    public void VerifyWithRotation_PreviousSecretAfterGrace_Fails()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":7}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("previous", payload, timestamp);

        Assert.False(WebhookSignature.VerifyWithRotation(
            "current", "previous", DateTime.UtcNow.AddHours(-120),
            payload, timestamp, signature, Grace, Window));
    }

    [Fact]
    public void VerifyWithRotation_UnknownSecret_Fails()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":7}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("attacker", payload, timestamp);

        Assert.False(WebhookSignature.VerifyWithRotation(
            "current", "previous", DateTime.UtcNow.AddHours(-1),
            payload, timestamp, signature, Grace, Window));
    }

    [Fact]
    public void Receiver_FirstDelivery_Valid_SecondIsDuplicate()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":9}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("secret", payload, timestamp);
        var store = new InMemoryDeliveryIdStore();
        const string deliveryId = "delivery-1";

        var first = WebhookReceiver.Validate(
            "secret", null, null, payload, timestamp, signature, deliveryId, store, Grace, Window);
        var second = WebhookReceiver.Validate(
            "secret", null, null, payload, timestamp, signature, deliveryId, store, Grace, Window);

        Assert.Equal(WebhookValidationResult.Valid, first);
        Assert.Equal(WebhookValidationResult.DuplicateDelivery, second);
    }

    [Fact]
    public void Receiver_InvalidSignature_DoesNotConsumeDeliveryId()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":10}");
        var timestamp = FreshTimestamp();
        var badSignature = WebhookSignature.ComputeWithTimestamp("wrong", payload, timestamp);
        var goodSignature = WebhookSignature.ComputeWithTimestamp("secret", payload, timestamp);
        var store = new InMemoryDeliveryIdStore();
        const string deliveryId = "delivery-2";

        var bad = WebhookReceiver.Validate(
            "secret", null, null, payload, timestamp, badSignature, deliveryId, store, Grace, Window);
        var good = WebhookReceiver.Validate(
            "secret", null, null, payload, timestamp, goodSignature, deliveryId, store, Grace, Window);

        Assert.Equal(WebhookValidationResult.SignatureMismatch, bad);
        Assert.Equal(WebhookValidationResult.Valid, good);
    }

    [Fact]
    public void Receiver_StaleDelivery_DoesNotConsumeDeliveryId()
    {
        var payload = Encoding.UTF8.GetBytes("{\"id\":11}");
        var staleTimestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600).ToString();
        var staleSignature = WebhookSignature.ComputeWithTimestamp("secret", payload, staleTimestamp);
        var store = new InMemoryDeliveryIdStore();
        const string deliveryId = "delivery-3";

        var stale = WebhookReceiver.Validate(
            "secret", null, null, payload, staleTimestamp, staleSignature, deliveryId, store, Grace, Window);

        Assert.Equal(WebhookValidationResult.StaleTimestamp, stale);
        Assert.False(store.Contains(deliveryId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Receiver_MissingSignature_Rejected(string signature)
    {
        var payload = Encoding.UTF8.GetBytes("{}");
        var store = new InMemoryDeliveryIdStore();

        var result = WebhookReceiver.Validate(
            "secret", null, null, payload, FreshTimestamp(), signature, "d-1", store, Grace, Window);

        Assert.Equal(WebhookValidationResult.MissingSignature, result);
    }

    [Fact]
    public void Receiver_MalformedTimestamp_Rejected()
    {
        var store = new InMemoryDeliveryIdStore();

        var result = WebhookReceiver.Validate(
            "secret", null, null, Encoding.UTF8.GetBytes("{}"),
            "not-a-number", "sha256=abc", "d-1", store, Grace, Window);

        Assert.Equal(WebhookValidationResult.MalformedTimestamp, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Receiver_MissingDeliveryId_Rejected(string deliveryId)
    {
        var payload = Encoding.UTF8.GetBytes("{}");
        var timestamp = FreshTimestamp();
        var signature = WebhookSignature.ComputeWithTimestamp("secret", payload, timestamp);
        var store = new InMemoryDeliveryIdStore();

        var result = WebhookReceiver.Validate(
            "secret", null, null, payload, timestamp, signature, deliveryId, store, Grace, Window);

        Assert.Equal(WebhookValidationResult.MissingDeliveryId, result);
    }

    [Fact]
    public void DeliveryStore_DuplicateClaim_ReturnsFalse()
    {
        var store = new InMemoryDeliveryIdStore();

        Assert.True(store.TryClaim("id-1"));
        Assert.False(store.TryClaim("id-1"));
        Assert.True(store.Contains("id-1"));
    }

    [Fact]
    public void DeliveryStore_ExpiredClaim_Reclaimable()
    {
        var store = new InMemoryDeliveryIdStore(TimeSpan.FromMilliseconds(50));

        Assert.True(store.TryClaim("id-expire"));
        Thread.Sleep(120);

        Assert.False(store.Contains("id-expire"));
        Assert.True(store.TryClaim("id-expire"));
    }
}
