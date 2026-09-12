using System.Net.Http.Json;

namespace Identity.API.IntegrationTests;

public class AuthTests : IClassFixture<IdentityApiFactory>
{
    private readonly HttpClient _client;

    public AuthTests(IdentityApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_And_Login_Succeeds()
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test1234567!";
        var fullName = "Test User";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FullName = fullName
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, registerResponse.StatusCode);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerResult);
        Assert.NotEqual(Guid.Empty, registerResult.UserId);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);
        Assert.NotEmpty(loginResult.AccessToken);
        Assert.NotEmpty(loginResult.RefreshToken);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "wrongpassword"
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownEmail_VerifyResendForgot_ReturnIndistinguishableSuccess()
    {
        // Enumeration defense: all three endpoints must answer unknown
        // addresses with the identical success shape.
        var email = $"unknown-{Guid.NewGuid()}@example.com";

        var verify = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new
        {
            Email = email,
            Token = "some-token"
        });
        var resend = await _client.PostAsJsonAsync("/api/v1/auth/resend-verification", new
        {
            Email = email
        });
        var forgot = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new
        {
            Email = email
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, verify.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, resend.StatusCode);
        Assert.Equal(System.Net.HttpStatusCode.OK, forgot.StatusCode);
        Assert.Equal(
            (await verify.Content.ReadAsStringAsync()).Length,
            (await resend.Content.ReadAsStringAsync()).Length);
        Assert.Equal(
            (await verify.Content.ReadAsStringAsync()).Length,
            (await forgot.Content.ReadAsStringAsync()).Length);
    }

    [Fact]
    public async Task Refresh_ConcurrentSameToken_ExactlyOneSucceeds()
    {
        var email = $"test-{Guid.NewGuid()}@example.com";
        var password = "Test1234567!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = email,
            Password = password,
            FullName = "Test User"
        });
        Assert.Equal(System.Net.HttpStatusCode.OK, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            Email = email,
            Password = password
        });
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResult);

        // Two rotations racing on the same token: the loser must not mint a
        // second live session — reuse (400) or conflict (409), never 200.
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = loginResult.RefreshToken }),
            _client.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = loginResult.RefreshToken }));

        Assert.Equal(1, responses.Count(r => r.StatusCode == System.Net.HttpStatusCode.OK));
        foreach (var loser in responses.Where(r => r.StatusCode != System.Net.HttpStatusCode.OK))
            Assert.True(
                loser.StatusCode is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict,
                $"unexpected loser status {loser.StatusCode}");
    }

    private record RegisterResponse(Guid UserId);
    private record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
}
