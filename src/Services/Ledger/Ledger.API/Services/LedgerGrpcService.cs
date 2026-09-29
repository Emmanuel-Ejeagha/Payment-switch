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
        var accounts = _db.LedgerAccounts.Where(a => a.MerchantId == merchantId);
        if (!string.IsNullOrWhiteSpace(request.Currency))
        {
            var currency = request.Currency.Trim().ToUpperInvariant();
            accounts = accounts.Where(a => a.Currency == currency);
        }

        var response = new GetBalancesResponse();
        foreach (var account in await accounts.OrderBy(a => a.Currency).ToListAsync())
        {
            response.Balances.Add(new Balance
            {
                Available = account.AvailableBalance,
                Pending = account.PendingBalance,
                Reserved = account.ReservedBalance,
                Currency = account.Currency
            });
        }

        return response;
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