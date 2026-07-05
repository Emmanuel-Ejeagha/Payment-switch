namespace Settlement.Application.DTOs;

public record PayoutDto(
    Guid MerchantId,
    decimal GrossVolume,
    decimal Fees,
    decimal NetAmount,
    string Currency
);