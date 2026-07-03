using BuildingBlocks.Shared.Results;
using Settlement.Application.DTOs;

namespace Settlement.Application.Interfaces;

public interface ILedgerService
{
    Task<Result<List<MerchantPayoutData>>> GetDailyPayoutDataAsync(DateTime date, CancellationToken cancellationToken = default);
}