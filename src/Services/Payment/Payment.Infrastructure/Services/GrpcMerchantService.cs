using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using PaymentSwitch.Protos.Merchant;

namespace Payment.Infrastructure.Services;

public class GrpcMerchantService : IMerchantService
{
    private readonly MerchantService.MerchantServiceClient _client;

    public GrpcMerchantService(MerchantService.MerchantServiceClient client)
    {
        _client = client;
    }

    public async Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var request = new GetMerchantStatusRequest { MerchantId = merchantId.ToString() };
        var response = await _client.GetMerchantStatusAsync(request, cancellationToken: cancellationToken);
        return Result<string>.Success(response.Status);
    }

    public async Task<Result<MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var request = new GetMerchantConfigRequest { MerchantId = merchantId.ToString() };
        var response = await _client.GetMerchantConfigAsync(request, cancellationToken: cancellationToken);
        return Result<MerchantConfig>.Success(new MerchantConfig(
            string.IsNullOrEmpty(response.WebhookUrl) ? null : response.WebhookUrl,
            response.AutoCapture));
    }

    public async Task<Result<MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default)
    {
        var request = new ResolveApiKeyRequest { KeyPrefix = keyPrefix, KeyValue = keyValue };
        var response = await _client.ResolveApiKeyAsync(request, cancellationToken: cancellationToken);
        if (string.IsNullOrEmpty(response.MerchantId))
            return new Error("Payment.InvalidApiKey", "Invalid API key.");

        return Result<MerchantKeyResolution>.Success(new MerchantKeyResolution(
            Guid.Parse(response.MerchantId),
            response.Status,
            response.Environment));
    }
}
