namespace Payment.Application.Features.Command.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id, Guid MerchantId);
