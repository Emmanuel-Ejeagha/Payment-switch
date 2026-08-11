using Notification.Application.Interfaces;
using PaymentSwitch.Protos.Merchant;

namespace Notification.Infrastructure.Services;

public class GrpcMerchantContactService : IMerchantContactService
{
    private readonly MerchantService.MerchantServiceClient _client;

    public GrpcMerchantContactService(MerchantService.MerchantServiceClient client)
    {
        _client = client;
    }

    public async Task<string?> GetMerchantEmailAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var request = new GetMerchantContactRequest { MerchantId = merchantId.ToString() };
        var response = await _client.GetMerchantContactAsync(request, cancellationToken: cancellationToken);
        return string.IsNullOrWhiteSpace(response.Email) ? null : response.Email;
    }
}
