using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;

namespace Payment.Application.Features.Queries.ListPlansByMerchant;

public class ListPlansByMerchantHandler
{
    private readonly IPlanRepository _repository;

    public ListPlansByMerchantHandler(IPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedData<PlanDto>>> Handle(ListPlansByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var plans = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountByMerchantAsync(query.MerchantId, cancellationToken);

        return new PagedData<PlanDto>(plans.Select(p => p.ToDto()).ToList(), total);
    }
}
