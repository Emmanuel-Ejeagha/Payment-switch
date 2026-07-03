namespace Settlement.Application.DTOs;

public record MerchantPayoutData(Guid MerchantId, decimal GrossVolume, decimal Fees, string Currency);