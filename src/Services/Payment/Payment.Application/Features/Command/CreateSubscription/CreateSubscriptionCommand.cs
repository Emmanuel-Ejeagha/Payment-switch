namespace Payment.Application.Features.Command.CreateSubscription;

public record CreateSubscriptionCommand(
    Guid MerchantId,
    Guid CustomerId,
    Guid PlanId,
    string CardToken,
    DateTime? StartAt = null);
