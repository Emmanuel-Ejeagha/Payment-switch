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

    public async Task<Result<List<MerchantDto>>> Handle(ListMerchantsQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {QueryName}", nameof(ListMerchantsQuery));

        var merchants = await _repository.ListAsync(query.Skip, query.Take, cancellationToken);
        return merchants;
    }
}
