using BuildingBlocks.Shared.Results;
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
}