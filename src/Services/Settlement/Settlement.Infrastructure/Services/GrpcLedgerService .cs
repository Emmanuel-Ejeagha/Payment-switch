using BuildingBlocks.Shared.Results;
using Settlement.Application.Interfaces;
using PaymentSwitch.Protos.Ledger;

namespace Settlement.Infrastructure.Services;

public class GrpcLedgerService : ILedgerService
{
    private readonly LedgerService.LedgerServiceClient _client;

    public GrpcLedgerService(LedgerService.LedgerServiceClient client)
    {
        _client = client;
    }

    public async Task<Result<List<Application.DTOs.MerchantPayoutData>>> GetDailyPayoutDataAsync(
        DateTime date, CancellationToken cancellationToken = default)
    {
        var request = new GetDailyPayoutDataRequest { Date = date.ToString("yyyy-MM-dd") };
        var response = await _client.GetDailyPayoutDataAsync(request, cancellationToken: cancellationToken);

        var data = response.Payouts.Select(p => new Application.DTOs.MerchantPayoutData(
            Guid.Parse(p.MerchantId),
            (decimal)p.GrossVolume,
            (decimal)p.Fees,
            p.Currency
        )).ToList();

        return Result<List<Application.DTOs.MerchantPayoutData>>.Success(data);
    }
}