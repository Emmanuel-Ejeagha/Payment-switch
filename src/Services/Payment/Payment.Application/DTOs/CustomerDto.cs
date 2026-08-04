namespace Payment.Application.DTOs;

public record CustomerDto(
    Guid Id,
    Guid MerchantId,
    string Code,
    string Email,
    string? Name,
    string? Phone,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
