namespace Settlement.Application.DTOs;

public record PayoutDto(
    Guid MerchantId,
    long GrossVolume,
    long Fees,
    long NetAmount,
    string Currency
);