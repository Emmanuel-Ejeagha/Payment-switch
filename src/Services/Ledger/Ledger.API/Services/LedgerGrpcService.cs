using BuildingBlocks.Shared.Auth;
using Grpc.Core;
using Ledger.Infrastructure.Persistence;
using Ledger.Infrastructure.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PaymentSwitch.Protos.Ledger;

namespace Ledger.API.Services;

[Authorize(Policy = AuthPolicies.ServiceOnly)]
public class LedgerGrpcService : LedgerService.LedgerServiceBase
{
    private readonly AppDbContext _db;
    private readonly IDailyPayoutQuery _dailyPayoutQuery;

    public LedgerGrpcService(AppDbContext db, IDailyPayoutQuery dailyPayoutQuery)
    {
        _db = db;
        _dailyPayoutQuery = dailyPayoutQuery;
    }

    public override async Task<GetBalancesResponse> GetBalances(
        GetBalancesRequest request, ServerCallContext context)
    {
        var merchantId = Guid.Parse(request.MerchantId);
        var account = await _db.LedgerAccounts
            .FirstOrDefaultAsync(a => a.MerchantId == merchantId);

        return new GetBalancesResponse
        {
            Available = account?.AvailableBalance ?? 0,
            Pending = account?.PendingBalance ?? 0,
            Reserved = account?.ReservedBalance ?? 0,
            Currency = account?.Currency ?? "USD"
        };
    }

    public override async Task<GetDailyPayoutDataResponse> GetDailyPayoutData(
        GetDailyPayoutDataRequest request, ServerCallContext context)
    {
        if (!DateTime.TryParse(request.Date, out var parsedDate))
            return new GetDailyPayoutDataResponse();

        var payouts = await _dailyPayoutQuery.GetAsync(parsedDate, context.CancellationToken);

        var resp = new GetDailyPayoutDataResponse();
        foreach (var payout in payouts)
        {
            resp.Payouts.Add(new MerchantPayoutData
            {
                MerchantId = payout.MerchantId.ToString(),
                GrossVolume = payout.GrossVolume,
                Fees = payout.Fees,
                Currency = payout.Currency
            });
        }
        return resp;
    }
}