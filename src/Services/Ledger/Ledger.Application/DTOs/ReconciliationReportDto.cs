using Ledger.Domain.Entities;

namespace Ledger.Application.DTOs;

public record ReconciliationLineItemDto(
    Guid MerchantId,
    string Currency,
    long ExpectedAvailable,
    long ActualAvailable,
    long ExpectedPending,
    long ActualPending,
    long ExpectedReserved,
    long ActualReserved,
    bool IsMatch);

public record ReconciliationReportDto(
    Guid Id,
    DateTime RunAtUtc,
    ReconciliationStatus Status,
    int TotalAccounts,
    int MismatchCount,
    IReadOnlyList<ReconciliationLineItemDto> Items);