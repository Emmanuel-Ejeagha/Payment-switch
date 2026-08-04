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

    public async Task<Result<List<SubscriptionDto>>> Handle(ListSubscriptionsByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);

        return Result<List<SubscriptionDto>>.Success(subscriptions.Select(s => s.ToDto()).ToList());
    }
}
