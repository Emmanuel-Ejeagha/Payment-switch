using System.Net.Http.Json;

namespace Merchant.API.IntegrationTests;

public class MerchantTests : IClassFixture<MerchantApiFactory>
{
    private readonly HttpClient _client;

    public MerchantTests(MerchantApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task OnboardMerchant_Anonymous_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/merchants", new
        {
            BusinessName = "Test Corp",
            Email = $"corp-{Guid.NewGuid()}@example.com"
        });

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OnboardMerchant_VerifiedOwner_Succeeds()
    {
        var ownerId = Guid.NewGuid();
        var email = $"corp-{Guid.NewGuid()}@example.com";
        var client = CreateAuthedClient(ownerId, email, emailVerified: true);
        var response = await client.PostAsJsonAsync("/api/v1/merchants", new
        {
            BusinessName = "Test Corp",
            Email = email
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OnboardResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.MerchantId);
    }

    [Fact]
    public async Task OnboardMerchant_UnverifiedOwner_Fails()
    {
        var ownerId = Guid.NewGuid();
        var email = $"corp-{Guid.NewGuid()}@example.com";
        var client = CreateAuthedClient(ownerId, email, emailVerified: false);
        var response = await client.PostAsJsonAsync("/api/v1/merchants", new
        {
            BusinessName = "Test Corp",
            Email = email
        });

        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateAuthedClient(Guid ownerId, string email, bool emailVerified)
    {
        var token = TestTokenFactory.CreateUserToken(ownerId, email, emailVerified);
        var client = _client;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private record OnboardResponse(Guid MerchantId);
}
