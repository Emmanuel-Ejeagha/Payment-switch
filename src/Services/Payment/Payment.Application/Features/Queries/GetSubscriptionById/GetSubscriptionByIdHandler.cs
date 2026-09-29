using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;

namespace Payment.Application.Features.Queries.GetSubscriptionById;

public class GetSubscriptionByIdHandler
{
    private readonly ISubscriptionRepository _repository;

    public GetSubscriptionByIdHandler(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var subscription = await _repository.GetByIdAsync(query.SubscriptionId, cancellationToken);
        if (subscription is null)
            return PaymentErrors.SubscriptionNotFound(query.SubscriptionId);

        return subscription.ToDto();
    }
}
