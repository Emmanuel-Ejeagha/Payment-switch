using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using PaymentSwitch.IntegrationTests.Shared;

namespace Notification.API.IntegrationTests;

public class NotificationListTests : IClassFixture<NotificationApiFactory>
{
    private readonly NotificationApiFactory _factory;

    public NotificationListTests(NotificationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_ReturnsOkWithTotalCountHeader()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/notifications?take=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(IsJsonArray(body), $"Expected a JSON array body, got: {body}");
        Assert.True(response.Headers.Contains("X-Total-Count"), "X-Total-Count header is missing");
    }

    [Fact]
    public async Task List_WithNegativeSkipAndZeroTake_ClampsAndReturnsOk()
    {
        var client = await CreateAdminClientAsync();

        var response = await client.GetAsync("/api/v1/notifications?skip=-5&take=0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateAdminToken());
        return client;
    }

    private static string CreateAdminToken()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "admin@paymentswitch.com"),
            new(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecrets.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "NotificationService",
            audience: TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsJsonArray(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind == JsonValueKind.Array;
    }
}
