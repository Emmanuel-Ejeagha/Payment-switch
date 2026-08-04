namespace Payment.Application.Features.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id, Guid MerchantId);
