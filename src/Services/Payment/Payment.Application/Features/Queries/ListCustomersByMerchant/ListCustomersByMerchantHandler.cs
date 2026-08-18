using BuildingBlocks.Shared.Paging;
using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;

namespace Payment.Application.Features.Queries.ListCustomersByMerchant;

public class ListCustomersByMerchantHandler
{
    private readonly ICustomerRepository _repository;

    public ListCustomersByMerchantHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedData<CustomerDto>>> Handle(ListCustomersByMerchantQuery query, CancellationToken cancellationToken = default)
    {
        var customers = await _repository.ListByMerchantAsync(query.MerchantId, query.Skip, query.Take, cancellationToken);
        var total = await _repository.CountByMerchantAsync(query.MerchantId, cancellationToken);

        return new PagedData<CustomerDto>(customers.Select(c => c.ToDto()).ToList(), total);
    }
}
