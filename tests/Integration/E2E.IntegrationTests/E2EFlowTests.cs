using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Identity.Application.Commands.Auth.Login;
using Identity.Application.Commands.Auth.Register;
using LedgerAppDbContext = Ledger.Infrastructure.Persistence.AppDbContext;
using Merchant.Application.Features.Commands.GenerateMerchantApiKey;
using Merchant.Application.Features.Commands.OnboardMerchant;
using Microsoft.EntityFrameworkCore;
using NotificationAppDbContext = Notification.Infrastructure.Persistence.AppDbContext;
using Payment.Application.DTOs;
using Payment.Application.Features.Command.CapturePayment;
using Payment.Application.Features.Command.ConfirmPaymentIntent;
using Settlement.Application.DTOs;

namespace E2E.IntegrationTests;

/// <summary>
/// The core E2E flow: registration and email verification through Identity,
/// merchant onboarding/approval/activation, a test secret key, a public payment
/// intent authorized then captured, the Ledger posting (with fees), the merchant
/// notification, and finally a settlement batch that nets the captured amount.
/// </summary>
public class E2EFlowTests : IClassFixture<E2EFactory>
{
    private const string AdminEmail = "admin@paymentswitch.com";
    private const string AdminPassword = "integration-test-admin-password";

    private readonly E2EFactory _factory;

    public E2EFlowTests(E2EFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullLifecycle_FromRegistrationToSettlement_ProducesExpectedLedgerAndBatchTotals()
    {
        var identity = _factory.IdentityHost.CreateClient();
        var merchantUser = _factory.MerchantHost.CreateClient();
        var merchantAdmin = _factory.MerchantHost.CreateClient();
        var paymentPublic = _factory.PaymentHost.CreateClient();
        var paymentInternal = _factory.PaymentHost.CreateClient();
        var settlement = _factory.SettlementHost.CreateClient();

        var email = $"e2e-{Guid.NewGuid():N}@example.com";
        const string password = "E2ePass1234!";

        // 1. Register, verify (via the captured plaintext token), login as owner.
        var register = await identity.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = password, FullName = "E2E Owner" });
        register.EnsureSuccessStatusCode();

        var verifyToken = ExtractVerificationToken(email);
        var verify = await identity.PostAsJsonAsync("/api/v1/auth/verify-email", new { Email = email, Token = verifyToken });
        verify.EnsureSuccessStatusCode();

        var login = await identity.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        login.EnsureSuccessStatusCode();
        var userToken = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        merchantUser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        paymentInternal.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        // 2. Onboard the merchant.
        var onboard = await merchantUser.PostAsJsonAsync("/api/v1/merchants", new { BusinessName = "E2E Merchant", Email = email });
        onboard.EnsureSuccessStatusCode();
        var merchantId = (await onboard.Content.ReadFromJsonAsync<OnboardMerchantResponse>())!.MerchantId;

        // 3. Wait for the Ledger to open the merchant's USD account (via merchant.events).
        await WaitUntilAsync(async () =>
        {
            await using var db = new LedgerAppDbContext(DbOptions<LedgerAppDbContext>(_factory.LedgerDbConnectionString));
            return await db.LedgerAccounts.AnyAsync(a => a.MerchantId == merchantId);
        }, "ledger account for merchant");

        // 4. Admin approves and activates the merchant.
        var adminLogin = await identity.PostAsJsonAsync("/api/v1/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        adminLogin.EnsureSuccessStatusCode();
        var adminToken = (await adminLogin.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        merchantAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        settlement.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        (await merchantAdmin.PostAsync($"/api/v1/merchants/{merchantId}/approve", null)).EnsureSuccessStatusCode();
        (await merchantAdmin.PostAsync($"/api/v1/merchants/{merchantId}/activate", null)).EnsureSuccessStatusCode();

        // 5. Disable auto-capture so the payment stays Authorized until captured.
        var config = await merchantUser.PutAsJsonAsync($"/api/v1/merchants/{merchantId}/configuration", new { AutoCapture = false });
        config.EnsureSuccessStatusCode();

        // 6. Generate a test secret key.
        var keyResponse = await merchantUser.PostAsJsonAsync($"/api/v1/merchants/{merchantId}/apikeys", new { Environment = "test" });
        keyResponse.EnsureSuccessStatusCode();
        var apiKey = (await keyResponse.Content.ReadFromJsonAsync<GenerateMerchantApiKeyResponse>())!.PlainTextKey;

        // 7. Create a payment intent through the public API (Visa, clean authorize).
        paymentPublic.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        paymentPublic.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));
        var intentResponse = await paymentPublic.PostAsJsonAsync("/v1/payments/intents", new
        {
            Amount = 10000L,
            Currency = "USD",
            PaymentMethod = "Card",
            CardLastFour = "4242",
            CardBrand = "Visa"
        });
        intentResponse.EnsureSuccessStatusCode();
        var intent = await intentResponse.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.Equal("Authorized", intent!.Status);

        // 8. Wait for the Ledger to reserve the authorized amount before capturing,
        //    so the capture can never race ahead of the authorize posting.
        await WaitUntilAsync(async () =>
        {
            await using var db = new LedgerAppDbContext(DbOptions<LedgerAppDbContext>(_factory.LedgerDbConnectionString));
            var account = await db.LedgerAccounts.FirstOrDefaultAsync(a => a.MerchantId == merchantId);
            return account is { PendingBalance: 10000, ReservedBalance: 10000 };
        }, "ledger reserve of 10000 for the authorized payment");

        // 9. Capture the authorized payment.
        var captureResponse = await paymentInternal.PostAsJsonAsync($"/api/v1/payments/{intent.IntentId}/capture", new { });
        captureResponse.EnsureSuccessStatusCode();
        var capture = await captureResponse.Content.ReadFromJsonAsync<CapturePaymentResponse>();
        Assert.Equal("Captured", capture!.Status);

        // 10. Ledger reflects 10000 captured minus the 150 bps fee (150) = 9850 available.
        await WaitUntilAsync(async () =>
        {
            await using var db = new LedgerAppDbContext(DbOptions<LedgerAppDbContext>(_factory.LedgerDbConnectionString));
            var account = await db.LedgerAccounts.FirstOrDefaultAsync(a => a.MerchantId == merchantId);
            return account is { AvailableBalance: 9850, PendingBalance: 0, ReservedBalance: 0 };
        }, "ledger available balance of 9850");

        // 11. The merchant owner receives a payment-captured notification.
        await WaitUntilAsync(async () =>
        {
            await using var db = new NotificationAppDbContext(DbOptions<NotificationAppDbContext>(_factory.NotificationDbConnectionString));
            return await db.Notifications.AnyAsync(n => n.Recipient == email && n.Subject == "Payment Captured");
        }, "payment captured notification");

        // 12. A settlement batch for today nets 10000 - 150 = 9850.
        var trigger = await settlement.PostAsJsonAsync("/api/v1/settlement/trigger", new { BatchDate = DateTime.UtcNow.Date });
        trigger.EnsureSuccessStatusCode();
        var triggerResult = await trigger.Content.ReadFromJsonAsync<TriggerSettlementResponse>();
        var batch = await settlement.GetFromJsonAsync<SettlementBatchDto>($"/api/v1/settlement/{triggerResult!.Id}");
        Assert.Equal(9850, batch!.TotalAmount);
    }

    [Fact]
    public async Task PublicPayments_RequiresIdempotencyKey_AndReplaysSafely()
    {
        var (_, apiKey) = await ProvisionMerchantAsync();

        // 1. Create without an Idempotency-Key header -> 400.
        var noKey = PublicClient(apiKey);
        var missing = await noKey.PostAsJsonAsync("/v1/payments/intents", new
        {
            Amount = 10000L,
            Currency = "USD",
            PaymentMethod = "Card",
            CardLastFour = "4242",
            CardBrand = "Visa"
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, missing.StatusCode);

        // 2. Create with a key -> 200; replaying the same key returns the same intent.
        var key = Guid.NewGuid().ToString("N");
        var withKey = PublicClient(apiKey, key);
        var first = await withKey.PostAsJsonAsync("/v1/payments/intents", new
        {
            Amount = 10000L,
            Currency = "USD",
            PaymentMethod = "Card",
            CardLastFour = "4242",
            CardBrand = "Visa"
        });
        first.EnsureSuccessStatusCode();
        var firstIntent = await first.Content.ReadFromJsonAsync<PaymentIntentResponse>();

        var replay = await withKey.PostAsJsonAsync("/v1/payments/intents", new
        {
            Amount = 10000L,
            Currency = "USD",
            PaymentMethod = "Card",
            CardLastFour = "4242",
            CardBrand = "Visa"
        });
        replay.EnsureSuccessStatusCode();
        var replayed = await replay.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.Equal(firstIntent.IntentId, replayed!.IntentId);

        // 3. A 3DS card produces RequiresAction; confirm without a key -> 400, with one -> 200.
        var challengeKey = Guid.NewGuid().ToString("N");
        var challengeClient = PublicClient(apiKey, challengeKey);
        var challenge = await challengeClient.PostAsJsonAsync("/v1/payments/intents", new
        {
            Amount = 10000L,
            Currency = "USD",
            PaymentMethod = "Card",
            CardLastFour = "3001",
            CardBrand = "Visa"
        });
        challenge.EnsureSuccessStatusCode();
        var challengeIntent = await challenge.Content.ReadFromJsonAsync<PaymentIntentResponse>();
        Assert.Equal("RequiresAction", challengeIntent!.Status);

        var confirmNoKey = PublicClient(apiKey);
        var confirmMissing = await confirmNoKey.PostAsJsonAsync($"/v1/payments/{challengeIntent.IntentId}/confirm", new { });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, confirmMissing.StatusCode);

        var confirmKey = Guid.NewGuid().ToString("N");
        var confirmClient = PublicClient(apiKey, confirmKey);
        var confirm = await confirmClient.PostAsJsonAsync($"/v1/payments/{challengeIntent.IntentId}/confirm", new { });
        confirm.EnsureSuccessStatusCode();
        var confirmed = await confirm.Content.ReadFromJsonAsync<ConfirmPaymentIntentResponse>();

        // 4. Replaying confirm with the SAME key returns the original result, not an error.
        var confirmReplay = await confirmClient.PostAsJsonAsync($"/v1/payments/{challengeIntent.IntentId}/confirm", new { });
        confirmReplay.EnsureSuccessStatusCode();
        var replayedConfirm = await confirmReplay.Content.ReadFromJsonAsync<ConfirmPaymentIntentResponse>();
        Assert.Equal(confirmed!.Status, replayedConfirm!.Status);
    }

    private HttpClient PublicClient(string apiKey, string? idempotencyKey = null)
    {
        var client = _factory.PaymentHost.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        if (idempotencyKey is not null)
            client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey);
        return client;
    }

    private async Task<(Guid MerchantId, string ApiKey)> ProvisionMerchantAsync()
    {
        var identity = _factory.IdentityHost.CreateClient();
        var merchantUser = _factory.MerchantHost.CreateClient();
        var merchantAdmin = _factory.MerchantHost.CreateClient();

        var email = $"e2e-idem-{Guid.NewGuid():N}@example.com";
        const string password = "E2ePass1234!";

        var register = await identity.PostAsJsonAsync("/api/v1/auth/register", new { Email = email, Password = password, FullName = "E2E Idempotency Owner" });
        register.EnsureSuccessStatusCode();

        var verifyToken = ExtractVerificationToken(email);
        var verify = await identity.PostAsJsonAsync("/api/v1/auth/verify-email", new { Email = email, Token = verifyToken });
        verify.EnsureSuccessStatusCode();

        var login = await identity.PostAsJsonAsync("/api/v1/auth/login", new { Email = email, Password = password });
        login.EnsureSuccessStatusCode();
        var userToken = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;

        merchantUser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

        var onboard = await merchantUser.PostAsJsonAsync("/api/v1/merchants", new { BusinessName = "E2E Idempotency Merchant", Email = email });
        onboard.EnsureSuccessStatusCode();
        var merchantId = (await onboard.Content.ReadFromJsonAsync<OnboardMerchantResponse>())!.MerchantId;

        var adminLogin = await identity.PostAsJsonAsync("/api/v1/auth/login", new { Email = AdminEmail, Password = AdminPassword });
        adminLogin.EnsureSuccessStatusCode();
        var adminToken = (await adminLogin.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        merchantAdmin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        (await merchantAdmin.PostAsync($"/api/v1/merchants/{merchantId}/approve", null)).EnsureSuccessStatusCode();
        (await merchantAdmin.PostAsync($"/api/v1/merchants/{merchantId}/activate", null)).EnsureSuccessStatusCode();

        var keyResponse = await merchantUser.PostAsJsonAsync($"/api/v1/merchants/{merchantId}/apikeys", new { Environment = "test" });
        keyResponse.EnsureSuccessStatusCode();
        var apiKey = (await keyResponse.Content.ReadFromJsonAsync<GenerateMerchantApiKeyResponse>())!.PlainTextKey;

        return (merchantId, apiKey);
    }

    private string ExtractVerificationToken(string email)
    {
        var message = _factory.Emails.Sent.Single(m => m.To == email);
        var match = Regex.Match(message.TextBody, @"Your verification token is: ([A-F0-9]+)|token=([A-F0-9]+)");
        Assert.True(match.Success, $"No verification token in email body: {message.TextBody}");
        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }
    private static DbContextOptions<T> DbOptions<T>(string connectionString) where T : DbContext
    => new DbContextOptionsBuilder<T>().UseNpgsql(connectionString).Options;

    private static async Task WaitUntilAsync(Func<Task<bool>> predicate, string what, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(30));
        while (DateTime.UtcNow < deadline)
        {
            if (await predicate())
                return;
            await Task.Delay(500);
        }

        Assert.Fail($"Timed out waiting for {what}.");
    }
}