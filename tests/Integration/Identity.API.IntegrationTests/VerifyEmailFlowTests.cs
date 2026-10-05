using System.Net.Http.Json;
using System.Text.Json;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.API.IntegrationTests;

/// <summary>
/// Phase 3/4 exit criteria: proves the register → verify → login flow works
/// end-to-end through the real Identity API and database. Delivery is
/// event-driven (outbox → Notification/Resend): registering must write an
/// `EmailVerificationRequestedDomainEvent` outbox row carrying the single-use
/// token, which confirms the account; logins report email confirmation.
/// </summary>
public class VerifyEmailFlowTests : IClassFixture<IdentityApiFactory>
{
    private readonly IdentityApiFactory _factory;
    private readonly HttpClient _client;

    public VerifyEmailFlowTests(IdentityApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_Verify_Login_FullFlow()
    {
        var email = $"verify-{Guid.NewGuid()}@example.com";
        var password = "Test123456!";

        var userId = await RegisterAsync(email, password);

        // Registering must have published a verification event whose payload
        // carries the single-use token, and the account starts unverified.
        var token = await AssertTokenFromAsync(email);
        Assert.False(await IsVerifiedAsync(userId));

        // Login before verification reports the account as unconfirmed.
        var loginBefore = await LoginAsync(email, password);
        Assert.False(loginBefore.EmailConfirmed);

        // Verify with the captured token → sealed.
        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = token
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, verify.StatusCode);
        Assert.True(await IsVerifiedAsync(userId));

        // Single-use: verifying again with the same token must be refused.
        var duplicate = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = token
        });
        Assert.Equal(System.Net.HttpStatusCode.Conflict, duplicate.StatusCode);

        // Login after verification reports the account as confirmed.
        var loginAfter = await LoginAsync(email, password);
        Assert.True(loginAfter.EmailConfirmed);
    }

    [Fact]
    public async Task Register_WritesVerificationEventToOutbox()
    {
        var email = $"outbox-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");

        var payloads = await OutboxPayloadsAsync(email);
        var payload = Assert.Single(payloads);
        Assert.Equal(email, payload.GetProperty("Email").GetString());
        Assert.NotEmpty(payload.GetProperty("Token").GetString());
        Assert.True(payload.GetProperty("ExpiresAtUtc").GetDateTime() > DateTime.UtcNow);
    }

    [Fact]
    public async Task Verify_WithInvalidToken_Returns400()
    {
        var email = $"invalid-token-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = new string('0', 64) // Deliberately all-zeros: an invalid-token fixture, never a real token.
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resend_IssuesNewToken_AndInvalidatesTheOldOne()
    {
        var email = $"resend-{Guid.NewGuid()}@example.com";
        var userId = await RegisterAsync(email, "Test123456!");
        var firstToken = await AssertTokenFromAsync(email);

        // Move the last send outside the cooldown window (default 60s) so the
        // resend is accepted, as it would be for a real delayed retry.
        await BackdateLastSendAsync(userId, DateTime.UtcNow.AddMinutes(-5));

        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new { Email = email });
        Assert.Equal(System.Net.HttpStatusCode.OK, resend.StatusCode);

        var secondToken = await AssertLatestTokenFromAsync(email);
        Assert.NotEqual(firstToken, secondToken);

        // The previous token was invalidated by the resend.
        var stale = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = firstToken
        });
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, stale.StatusCode);

        // The current token verifies the account.
        var fresh = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = secondToken
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, fresh.StatusCode);
    }

    [Fact]
    public async Task Resend_WithinCooldown_Returns429AndSendsNothing()
    {
        var email = $"cooldown-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");
        var before = await OutboxPayloadsAsync(email);

        // Registration just sent: an immediate resend must be throttled and
        // must not rotate the token (no new outbox row).
        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new { Email = email });
        Assert.Equal(System.Net.HttpStatusCode.TooManyRequests, resend.StatusCode);

        var after = await OutboxPayloadsAsync(email);
        Assert.Equal(before.Count, after.Count);
    }

    [Fact]
    public async Task Resend_UnknownEmail_Returns200WithoutDisclosing()
    {
        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification",
            new { Email = $"nobody-{Guid.NewGuid()}@example.com" });

        Assert.Equal(System.Net.HttpStatusCode.OK, resend.StatusCode);
    }

    [Fact]
    public async Task Resend_VerifiedEmail_Returns200WithoutDisclosing()
    {
        var email = $"verified-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");
        var token = await AssertTokenFromAsync(email);
        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new { Email = email, Token = token });
        Assert.Equal(System.Net.HttpStatusCode.OK, verify.StatusCode);

        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new { Email = email });

        Assert.Equal(System.Net.HttpStatusCode.OK, resend.StatusCode);
    }

    [Fact]
    public async Task Verify_WithExpiredToken_Returns400()
    {
        var email = $"expired-{Guid.NewGuid()}@example.com";
        var userId = await RegisterAsync(email, "Test123456!");
        var token = await AssertTokenFromAsync(email);

        // Force the token past its expiry in the database, as a real account
        // would be after the 24-hour window.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"Users\" SET \"EmailVerificationTokenExpiresAt\" = @p0 WHERE \"Id\" = @p1",
                DateTime.UtcNow.AddMinutes(-1), userId);
        }

        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = token
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> RegisterAsync(string email, string password)
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FullName = "Verify Flow User"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, registerResponse.StatusCode);
        var result = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(result);
        return result!.UserId;
    }

    private async Task<LoginResponse> LoginAsync(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        return result!;
    }

    private async Task<string> AssertTokenFromAsync(string email)
    {
        var payloads = await OutboxPayloadsAsync(email);
        var payload = Assert.Single(payloads);
        return TokenFrom(payload);
    }

    private async Task<string> AssertLatestTokenFromAsync(string email)
    {
        var payloads = await OutboxPayloadsAsync(email);
        Assert.True(payloads.Count >= 2, "Expected the resend to publish a second verification event.");
        return TokenFrom(payloads[^1]);
    }

    /// <summary>
    /// Reads the verification-event outbox rows for an address, oldest first.
    /// The raw token travels in the event payload (never in logs); tests are
    /// the only readers.
    /// </summary>
    private async Task<List<JsonElement>> OutboxPayloadsAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var rows = await db.OutboxMessages
            .Where(m => m.EventType == "EmailVerificationRequestedDomainEvent")
            .OrderBy(m => m.OccurredOn)
            .Select(m => m.Payload)
            .ToListAsync();
        return rows
            .Select(p => JsonDocument.Parse(p).RootElement)
            .Where(e => e.GetProperty("Email").GetString() == email)
            .ToList();
    }

    private static string TokenFrom(JsonElement payload)
    {
        var token = payload.GetProperty("Token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token), "Verification event did not carry a token.");
        return token!;
    }

    private async Task BackdateLastSendAsync(Guid userId, DateTime sentAtUtc)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"Users\" SET \"LastVerificationEmailSentAtUtc\" = @p0 WHERE \"Id\" = @p1",
            sentAtUtc, userId);
    }

    private async Task<bool> IsVerifiedAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.AnyAsync(u => u.Id == userId && u.EmailConfirmed);
    }

    private record RegisterResponse(Guid UserId);
    private record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, bool EmailConfirmed);
}