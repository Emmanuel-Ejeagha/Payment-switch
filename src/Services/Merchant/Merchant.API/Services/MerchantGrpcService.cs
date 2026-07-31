using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Merchant.Infrastructure.Persistence;
using PaymentSwitch.Protos.Merchant;

namespace Merchant.API.Services;

public class MerchantGrpcService : MerchantService.MerchantServiceBase
{
    private readonly AppDbContext _db;

    public MerchantGrpcService(AppDbContext db)
    {
        _db = db;
    }

    public override async Task<GetMerchantStatusResponse> GetMerchantStatus(
        GetMerchantStatusRequest request, ServerCallContext context)
    {
        var merchantId = Guid.Parse(request.MerchantId);
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId);

        return new GetMerchantStatusResponse
        {
            Status = merchant?.Status.Value ?? "unknown"
        };
    }

    public override async Task<GetMerchantConfigResponse> GetMerchantConfig(
        GetMerchantConfigRequest request, ServerCallContext context)
    {
        var merchantId = Guid.Parse(request.MerchantId);
        var merchant = await _db.Merchants.FirstOrDefaultAsync(m => m.Id == merchantId);

        var resp = new GetMerchantConfigResponse();
        if (merchant != null)
        {
            resp.WebhookUrl = merchant.WebhookUrl?.Value ?? string.Empty;
            resp.EnabledPaymentMethods.AddRange(merchant.EnabledPaymentMethods);
            resp.AutoCapture = merchant.AutoCapture;
        }
        return resp;
    }

    public override async Task<ResolveApiKeyResponse> ResolveApiKey(
        ResolveApiKeyRequest request, ServerCallContext context)
    {
        var resolution = await (from k in _db.MerchantApiKeys
                                join m in _db.Merchants on k.MerchantId equals m.Id
                                where k.KeyPrefix == request.KeyPrefix
                                      && k.KeyHash == request.KeyHash
                                      && k.RevokedAt == null
                                select new { m.Id, m.Status, k.Environment })
            .FirstOrDefaultAsync(context.CancellationToken);

        var response = new ResolveApiKeyResponse();
        if (resolution != null)
        {
            response.MerchantId = resolution.Id.ToString();
            response.Status = resolution.Status.Value;
            response.Environment = resolution.Environment;
        }
        return response;
    }
}