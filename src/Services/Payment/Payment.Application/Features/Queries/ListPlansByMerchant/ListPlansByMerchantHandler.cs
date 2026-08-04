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

    public async Task<Result<List<PlanDto>>> Handle(ListPlansByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var plans = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);

        return Result<List<PlanDto>>.Success(plans.Select(p => p.ToDto()).ToList());
    }
}
