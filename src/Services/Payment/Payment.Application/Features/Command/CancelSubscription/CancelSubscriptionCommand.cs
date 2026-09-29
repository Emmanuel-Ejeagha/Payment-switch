namespace Payment.Application.Features.Command.CancelSubscription;

/// <summary>
/// Cancels a subscription. When <paramref name="AtPeriodEnd"/> is true the current
/// period is allowed to run out and no further cycle is billed.
/// </summary>
public record CancelSubscriptionCommand(Guid SubscriptionId, bool AtPeriodEnd = false);
