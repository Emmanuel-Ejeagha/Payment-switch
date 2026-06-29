namespace Payment.Application.DTOs;

public record GatewayResponse(bool Success, string? AuthorizationCode, string? GatewayReference, string? ErrorMessage);
