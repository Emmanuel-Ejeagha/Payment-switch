namespace Ledger.Application.DTOs;

public record TransactionDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Currency,
    string Description,
    DateTime Timestamp
);