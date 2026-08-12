using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.API.IntegrationTests;

/// <summary>
/// Phase 3 exit criterion: proves the register → verify → login → gated-flow
/// works end-to-end through the real Identity API and database. Registering a
/// user must produce a verification email, the plaintext token from that email
/// must confirm the account (single-use), and API-key generation must be blocked
/// until the email is verified.
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
    public async Task Register_Verify_Login_AndGenerateApiKey_FullFlow()
    {
        var email = $"verify-{Guid.NewGuid()}@example.com";
        var password = "Test123456!";

        var userId = await RegisterAsync(email, password);

        // Registering must have emitted a verification email whose body carries
        // the single-use token, and the account starts unverified.
        var token = AssertTokenFrom(email);
        Assert.False(await IsVerifiedAsync(userId));

        // Gated: generating an API key before verification is refused.
        var accessToken = await LoginAsync(email, password);
        var blocked = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/ApiKeys")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
            Content = JsonContent.Create(new { Environment = "test" })
        });
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, blocked.StatusCode);

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

        // Gating lifts: API-key generation now succeeds.
        var allowed = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/api/v1/ApiKeys")
        {
            Headers = { Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken) },
            Content = JsonContent.Create(new { Environment = "test" })
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, allowed.StatusCode);
        var key = await allowed.Content.ReadFromJsonAsync<ApiKeyResponse>();
        Assert.NotNull(key);
        Assert.StartsWith("sk_test_", key!.PlainTextKey);
    }

    [Fact]
    public async Task Verify_WithInvalidToken_Returns400()
    {
        var email = $"invalid-token-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = "9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08"
        });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resend_IssuesNewToken_AndInvalidatesTheOldOne()
    {
        var email = $"resend-{Guid.NewGuid()}@example.com";
        await RegisterAsync(email, "Test123456!");
        var firstToken = AssertTokenFrom(email);

        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new { Email = email });
        Assert.Equal(System.Net.HttpStatusCode.OK, resend.StatusCode);

        var secondToken = AssertTokenFrom(email);
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
    public async Task Verify_WithExpiredToken_Returns400()
    {
        var email = $"expired-{Guid.NewGuid()}@example.com";
        var userId = await RegisterAsync(email, "Test123456!");
        var token = AssertTokenFrom(email);

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

    private async Task<string> LoginAsync(string email, string password)
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);
        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        return result!.AccessToken;
    }

    private string AssertTokenFrom(string email)
    {
        var message = IdentityApiFactory.Emails.Sent.LastOrDefault(m => m.To == email);
        Assert.NotNull(message);

        // The builder embeds the plaintext token either directly in the body
        // ("Your verification token is: ...") or in the verify link (token=...).
        var match = Regex.Match(message!.TextBody,
            @"Your verification token is: ([A-F0-9]+)|token=([A-F0-9]+)");
        Assert.True(match.Success, "Verification email did not carry a token.");
        var token = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        Assert.NotEmpty(token);
        return token;
    }

    private async Task<bool> IsVerifiedAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Users.AnyAsync(u => u.Id == userId && u.EmailConfirmed);
    }

    private record RegisterResponse(Guid UserId);
    private record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
    private record ApiKeyResponse(Guid KeyId, string PlainTextKey, string Environment, DateTime CreatedAt);
}