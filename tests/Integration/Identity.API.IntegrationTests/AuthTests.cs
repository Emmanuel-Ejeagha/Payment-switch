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
        var password = "Test123!";
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

    private record RegisterResponse(Guid UserId);
    private record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
}
