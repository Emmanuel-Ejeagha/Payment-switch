using System.Text.Json;

namespace Payment.API.IntegrationTests;

public class PaymentListTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public PaymentListTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_AuthenticatedUser_ReturnsArrayWithTotalCountHeader()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/payments?merchantId=" + Guid.NewGuid() + "&take=500");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Total-Count", out var values));
        Assert.NotNull(values);

        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
    }

    [Fact]
    public async Task List_OutOfRangePaging_ClampsBounds()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/payments?merchantId=" + Guid.NewGuid() + "&skip=-5&take=0");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", CreateUserToken());
        return client;
    }

    private static string CreateUserToken()
    {
        var claims = new List<System.Security.Claims.Claim>
        {
            new(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(System.Security.Claims.ClaimTypes.Email, $"user-{Guid.NewGuid()}@example.com"),
            new(BuildingBlocks.Shared.Auth.CustomClaimTypes.EmailVerified, "true")
        };

        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(PaymentSwitch.IntegrationTests.Shared.TestSecrets.JwtSecret));
        var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "PaymentService",
            audience: PaymentSwitch.IntegrationTests.Shared.TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}