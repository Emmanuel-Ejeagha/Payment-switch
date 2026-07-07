using BuildingBlocks.Shared.Results;

namespace Payment.Application.Interfaces;

public interface IMerchantService
{
    Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default);
}