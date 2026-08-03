namespace Ledger.Application.DTOs;

public record TransactionDto(
    Guid Id,
    string Type,
    long Amount,
    string Currency,
    string Description,
    DateTime Timestamp
);