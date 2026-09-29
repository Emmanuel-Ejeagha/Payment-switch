using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;

namespace Payment.Application.Features.Queries.ListSubscriptionsByMerchant;

public class ListSubscriptionsByMerchantHandler
{
    private readonly ISubscriptionRepository _repository;

    public ListSubscriptionsByMerchantHandler(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedData<SubscriptionDto>>> Handle(ListSubscriptionsByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountByMerchantAsync(query.MerchantId, cancellationToken);

        return new PagedData<SubscriptionDto>(subscriptions.Select(s => s.ToDto()).ToList(), total);
    }
}
