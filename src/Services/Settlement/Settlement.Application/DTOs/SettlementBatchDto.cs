namespace Settlement.Application.DTOs;

public record SettlementBatchDto(
    Guid Id,
    DateTime BatchDate,
    string Status,
    long TotalAmount,
    List<PayoutDto> Payouts
);