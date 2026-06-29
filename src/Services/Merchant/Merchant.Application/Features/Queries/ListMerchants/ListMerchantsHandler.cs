namespace Merchant.Application.Features.Queries.ListMerchants;

public class ListMerchantsHandler
{
    private readonly IMerchantRepository _repository;

    public ListMerchantsHandler(IMerchantRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<List<MerchantDto>>> Handle(ListMerchantsQuery query, CancellationToken cancellationToken = default)
    {
        var merchants = await _repository.ListAsync(query.Skip, query.Take, cancellationToken);
        return merchants;
    }
}
