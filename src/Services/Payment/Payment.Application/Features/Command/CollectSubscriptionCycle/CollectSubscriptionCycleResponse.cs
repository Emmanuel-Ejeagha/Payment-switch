namespace Payment.Application.Features.Command.CollectSubscriptionCycle;

/// <summary>
/// Outcome of one collection attempt.
/// </summary>
public record CollectSubscriptionCycleResponse(
    Guid SubscriptionId,
    Guid InvoiceId,
    string InvoiceStatus,
    string SubscriptionStatus,
    Guid? PaymentIntentId,
    bool Collected,
    string? Error);
