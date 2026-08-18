using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Microsoft.Extensions.Logging;

namespace Merchant.Application.Features.Queries.ListMerchants;

public class ListMerchantsHandler
{
    private readonly IMerchantRepository _repository;
    private readonly ILogger<ListMerchantsHandler> _logger;

    public ListMerchantsHandler(IMerchantRepository repository, ILogger<ListMerchantsHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<PagedData<MerchantDto>>> Handle(ListMerchantsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(ListMerchantsQuery));

        var merchants = await _repository.ListAsync(query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountAsync(cancellationToken);
        return new PagedData<MerchantDto>(merchants, total);
    }
}
