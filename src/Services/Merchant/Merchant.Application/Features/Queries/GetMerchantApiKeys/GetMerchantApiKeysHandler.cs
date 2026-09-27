using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Queries.GetMerchantApiKeys;

public class GetMerchantApiKeysHandler
{
    private readonly IMerchantRepository _repository;
    private readonly ILogger<GetMerchantApiKeysHandler> _logger;

    public GetMerchantApiKeysHandler(IMerchantRepository repository, ILogger<GetMerchantApiKeysHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<List<MerchantApiKeyDto>>> Handle(GetMerchantApiKeysQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName} for Merchant {MerchantId}", nameof(GetMerchantApiKeysQuery), query.MerchantId);

        var merchant = await _repository.GetByIdAsync(query.MerchantId, cancellationToken);
        if (merchant is null)
            return MerchantErrors.MerchantNotFound(query.MerchantId);

        if (!query.Caller.CanAccess(merchant.OwnerId))
            return MerchantErrors.Unauthorized();

        var keys = await _repository.GetApiKeysByMerchantIdAsync(query.MerchantId, cancellationToken);
        return Result<List<MerchantApiKeyDto>>.Success(keys);
    }
}
