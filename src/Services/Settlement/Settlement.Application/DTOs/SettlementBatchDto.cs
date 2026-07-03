namespace Settlement.Application.DTOs;

public record SettlementBatchDto(
    Guid Id,
    DateTime BatchDate,
    string Status,
    decimal TotalAmount,
    List<PayoutDto> Payouts
);