using System.Net;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace Payment.API.IntegrationTests;

public class VersioningBehaviorTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public VersioningBehaviorTests(PaymentApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExplicitV1_ResolvesRoute_AndReportsSupportedVersions()
    {
        var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/payments?merchantId=" + Guid.NewGuid() + "&take=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var supported));
        Assert.Contains("1.0", supported);
    }

    [Fact]
    public async Task VersionSegmentIsRequired_UnversionedRequestRejected()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/payments", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnknownMajorVersion_Rejected()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/v2/payments", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnsupportedMinorVersion_Rejected()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/v1.1/payments", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MalformedVersionSegment_Rejected()
    {
        var response = await _factory.CreateClient().PostAsync(
            "/api/v1.2.3/payments", new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
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
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, $"user-{Guid.NewGuid()}@example.com"),
            new(BuildingBlocks.Shared.Auth.CustomClaimTypes.EmailVerified, "true")
        };

        var key = new SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(PaymentSwitch.IntegrationTests.Shared.TestSecrets.JwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "PaymentService",
            audience: PaymentSwitch.IntegrationTests.Shared.TestSecrets.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
