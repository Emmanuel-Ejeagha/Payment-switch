namespace Settlement.Application.DTOs;

public record SettlementBatchDto(
    Guid Id,
    DateTime BatchDate,
    string Status,
    long TotalAmount,
    string? Currency,
    List<PayoutDto> Payouts
);