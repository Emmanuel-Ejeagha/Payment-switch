using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Application.Mappings;

public static class CustomerMappings
{
    public static CustomerDto ToDto(this Customer customer) => new(
        customer.Id,
        customer.MerchantId,
        customer.Code,
        customer.Email,
        customer.Name,
        customer.Phone,
        customer.Description,
        customer.CreatedAt,
        customer.UpdatedAt
    );
}
