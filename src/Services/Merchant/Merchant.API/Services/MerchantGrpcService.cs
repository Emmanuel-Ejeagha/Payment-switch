using BuildingBlocks.Shared.Auth;
using BuildingBlocks.Shared.Security;
using Grpc.Core;
using Merchant.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PaymentSwitch.Protos.Merchant;

namespace Merchant.API.Services;

[Authorize(Policy = AuthPolicies.ServiceOnly)]
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
            resp.WebhookSecret = merchant.WebhookSecret?.Value ?? string.Empty;
            resp.PreviousWebhookSecret = merchant.PreviousWebhookSecret?.Value ?? string.Empty;
            resp.WebhookSecretRotatedAt = merchant.WebhookSecretRotatedAtUtc is { } rotatedAt
                ? new DateTimeOffset(rotatedAt).ToUnixTimeSeconds()
                : 0;
        }
        return resp;
    }

    public override async Task<ResolveApiKeyResponse> ResolveApiKey(
        ResolveApiKeyRequest request, ServerCallContext context)
    {
        var candidates = await (from k in _db.MerchantApiKeys
                                join m in _db.Merchants on k.MerchantId equals m.Id
                                where k.KeyPrefix == request.KeyPrefix
                                      && k.RevokedAt == null
                                select new { m.Id, Status = m.Status.Value, k.Environment, k.KeyHash })
            .ToListAsync(context.CancellationToken);

        var match = candidates.FirstOrDefault(c => ApiKeyHasher.Verify(request.KeyValue, c.KeyHash));

        var response = new ResolveApiKeyResponse();
        if (match != null)
        {
            response.MerchantId = match.Id.ToString();
            response.Status = match.Status;
            response.Environment = match.Environment;
        }
        return response;
    }
}