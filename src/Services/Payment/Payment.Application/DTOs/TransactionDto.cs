namespace Payment.Application.DTOs;

public record TransactionDto(
    Guid Id,
    string Type,
    long Amount,
    string Currency,
    DateTime Timestamp
);