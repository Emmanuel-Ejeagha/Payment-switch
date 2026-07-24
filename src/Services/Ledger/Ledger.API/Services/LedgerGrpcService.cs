using Grpc.Core;
using Ledger.Domain.Entities;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using PaymentSwitch.Protos.Ledger;

namespace Ledger.API.Services;

public class LedgerGrpcService : LedgerService.LedgerServiceBase
{
    private readonly AppDbContext _db;

    public LedgerGrpcService(AppDbContext db)
    {
        _db = db;
    }

    public override async Task<GetBalancesResponse> GetBalances(
        GetBalancesRequest request, ServerCallContext context)
    {
        var merchantId = Guid.Parse(request.MerchantId);
        var account = await _db.LedgerAccounts
            .FirstOrDefaultAsync(a => a.MerchantId == merchantId);

        return new GetBalancesResponse
        {
            Available = (double)(account?.AvailableBalance ?? 0),
            Pending = (double)(account?.PendingBalance ?? 0),
            Reserved = (double)(account?.ReservedBalance ?? 0),
            Currency = account?.Currency ?? "USD"
        };
    }

    public override async Task<GetDailyPayoutDataResponse> GetDailyPayoutData(
        GetDailyPayoutDataRequest request, ServerCallContext context)
    {
        var date = DateTime.Parse(request.Date);
        var accounts = await _db.LedgerAccounts.ToListAsync();

        var resp = new GetDailyPayoutDataResponse();
        foreach (var account in accounts)
        {
            var gross = account.AvailableBalance; 
            var fees = 0m; 
            resp.Payouts.Add(new MerchantPayoutData
            {
                MerchantId = account.MerchantId.ToString(),
                GrossVolume = (double)gross,
                Fees = (double)fees,
                Currency = account.Currency
            });
        }
        return resp;
    }
}