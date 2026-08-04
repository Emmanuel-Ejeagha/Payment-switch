using BuildingBlocks.Shared.Results;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Application.Mappings;
using Payment.Domain;

namespace Payment.Application.Features.Queries.GetCustomerById;

public class GetCustomerByIdHandler
{
    private readonly ICustomerRepository _repository;

    public GetCustomerByIdHandler(ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<CustomerDto>> Handle(GetCustomerByIdQuery query, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (customer is null || customer.Deleted || customer.MerchantId != query.MerchantId)
            return PaymentErrors.CustomerNotFound(query.Id);

        return customer.ToDto();
    }
}
