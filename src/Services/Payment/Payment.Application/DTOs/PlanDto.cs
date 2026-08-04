namespace Payment.Application.DTOs;

public record PlanDto(
    Guid Id,
    Guid MerchantId,
    string Code,
    string Name,
    long Amount,
    string Currency,
    string IntervalUnit,
    int IntervalCount,
    string? Description,
    bool Active,
    DateTime CreatedAt);
