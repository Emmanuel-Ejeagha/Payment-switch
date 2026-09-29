using BuildingBlocks.Shared;
using BuildingBlocks.Shared.Results;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;

namespace Settlement.Infrastructure.Services;

public class MockLedgerService : ILedgerService
{
    public Task<Result<List<MerchantPayoutData>>> GetDailyPayoutDataAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var data = new List<MerchantPayoutData>
        {
            new(Guid.Parse("11111111-1111-1111-1111-111111111111"), 500000L, 10000L, "USD"),
            new(Guid.Parse("22222222-2222-2222-2222-222222222222"), 300000L, 6000L, "USD")
        };

        return Task.FromResult(Result<List<MerchantPayoutData>>.Success(data));
    }
}