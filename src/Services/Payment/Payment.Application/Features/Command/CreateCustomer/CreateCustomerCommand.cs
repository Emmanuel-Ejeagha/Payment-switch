namespace Payment.Application.Features.Command.CreateCustomer;

public record CreateCustomerCommand(
    Guid MerchantId,
    string Email,
    string? Name = null,
    string? Phone = null,
    string? Description = null
);
