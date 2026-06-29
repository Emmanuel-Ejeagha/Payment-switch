namespace Payment.Application.DTOs;

public record TransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    DateTime Timestamp
);