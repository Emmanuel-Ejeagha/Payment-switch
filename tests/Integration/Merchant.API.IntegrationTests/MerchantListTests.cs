using System.Text.Json;

namespace Merchant.API.IntegrationTests;

public class MerchantListTests : IClassFixture<MerchantApiFactory>
{
    private readonly MerchantApiFactory _factory;

    public MerchantListTests(MerchantApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_Admin_ReturnsArrayWithTotalCountHeader()
    {
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/merchants?take=500");

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
        var client = CreateAdminClient();
        var response = await client.GetAsync("/api/v1/merchants?skip=-5&take=0");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    private HttpClient CreateAdminClient()
    {
        var token = TestTokenFactory.CreateUserToken(Guid.NewGuid(), $"admin-{Guid.NewGuid()}@example.com", emailVerified: true, "Admin");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}