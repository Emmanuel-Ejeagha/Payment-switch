namespace Payment.Application.DTOs;

public record PaymentIntentResponse(Guid IntentId, string Status, string? ClientSecret);
