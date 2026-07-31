namespace Payment.Application.DTOs;

public record MerchantKeyResolution(
    Guid MerchantId,
    string Status,
    string Environment
);
