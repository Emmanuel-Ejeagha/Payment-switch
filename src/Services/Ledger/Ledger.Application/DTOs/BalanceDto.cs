namespace Ledger.Application.DTOs;

public record BalanceDto(
    Guid MerchantId,
    decimal Available,
    decimal Pending,
    decimal Reserved,
    string Currency
);