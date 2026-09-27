using System.Net.Http.Headers;
using System.Text.Json;

namespace Notification.API.Services;

public interface IMerchantGroupResolver
{
    Task<Guid?> ResolveMerchantIdAsync(string email, string accessToken, CancellationToken cancellationToken = default);
}

public class MerchantGroupResolver : IMerchantGroupResolver
{
    private readonly HttpClient _httpClient;

    public MerchantGroupResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Guid?> ResolveMerchantIdAsync(string email, string accessToken, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/v1/merchants/by-email/{Uri.EscapeDataString(email)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        using var json = await response.Content.ReadAsStreamAsync(cancellationToken);
        var merchant = await JsonSerializer.DeserializeAsync<MerchantGroupResponse>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        return merchant?.Id;
    }

    private sealed record MerchantGroupResponse(Guid Id);
}
