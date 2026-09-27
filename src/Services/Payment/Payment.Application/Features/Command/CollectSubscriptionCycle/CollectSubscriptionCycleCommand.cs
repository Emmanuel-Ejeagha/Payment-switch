namespace Payment.Application.Features.Command.CollectSubscriptionCycle;

/// <summary>
/// Issues (or reuses) the invoice for a subscription's current billing period and
/// attempts to collect it against the stored card token. Safe to retry.
/// </summary>
public record CollectSubscriptionCycleCommand(Guid SubscriptionId);
