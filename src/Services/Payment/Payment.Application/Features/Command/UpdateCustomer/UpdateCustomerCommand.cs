namespace Payment.Application.Features.Command.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    Guid MerchantId,
    string? Email = null,
    string? Name = null,
    string? Phone = null,
    string? Description = null
);
