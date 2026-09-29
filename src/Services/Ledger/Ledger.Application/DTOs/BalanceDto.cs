namespace Ledger.Application.DTOs;

public record BalanceDto(
    Guid MerchantId,
    long Available,
    long Pending,
    long Reserved,
    string Currency
);