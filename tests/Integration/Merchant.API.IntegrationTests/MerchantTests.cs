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
    public async Task OnboardMerchant_Succeeds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/merchants", new
        {
            BusinessName = "Test Corp",
            Email = $"corp-{Guid.NewGuid()}@example.com"
        });

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<OnboardResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.MerchantId);
    }

    private record OnboardResponse(Guid MerchantId);
}
