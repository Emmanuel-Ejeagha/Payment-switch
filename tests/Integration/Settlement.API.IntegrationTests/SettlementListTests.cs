using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PaymentSwitch.IntegrationTests.Shared;

namespace Settlement.API.IntegrationTests;

public class SettlementListTests : IClassFixture<SettlementApiFactory>
{
    private readonly HttpClient _client;

    public SettlementListTests(SettlementApiFactory factory)
    {
        _client = CreateAdminClient(factory);
    }

    [Fact]
    public async Task List_WithLargeTake_ReturnsOkWithTotalCountHeader()
    {
        var response = await _client.GetAsync("/api/v1/settlement?take=500");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);

        Assert.True(response.Headers.Contains("X-Total-Count"));
    }

    [Fact]
    public async Task List_WithNegativeSkipAndZeroTake_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/settlement?skip=-5&take=0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static HttpClient CreateAdminClient(SettlementApiFactory factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAdminToken());
        return client;
    }

    private static string CreateAdminToken()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "admin@example.com"),
            new(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecrets.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "SettlementService",
            audience: TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
