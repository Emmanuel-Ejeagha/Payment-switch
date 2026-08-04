namespace Payment.Application.Features.Command.CreatePlan;

public record CreatePlanCommand(
    Guid MerchantId,
    string Name,
    long Amount,
    string Currency,
    string IntervalUnit,
    int IntervalCount = 1,
    string? Description = null);
