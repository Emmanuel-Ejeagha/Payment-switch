namespace Settlement.Application.DTOs;

public record MerchantPayoutData(Guid MerchantId, long GrossVolume, long Fees, string Currency);