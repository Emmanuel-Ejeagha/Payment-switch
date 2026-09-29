using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IMerchantService
{
    Task<Result<string>> GetMerchantStatusAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<Result<MerchantConfig>> GetMerchantConfigAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<Result<Guid?>> GetMerchantOwnerAsync(Guid merchantId, CancellationToken cancellationToken = default);
    Task<Result<MerchantKeyResolution>> ResolveApiKeyAsync(string keyPrefix, string keyValue, CancellationToken cancellationToken = default);
}
