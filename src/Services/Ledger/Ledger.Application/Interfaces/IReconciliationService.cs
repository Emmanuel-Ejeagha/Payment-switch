using Ledger.Domain.Entities;

namespace Ledger.Application.Interfaces;

/// <summary>
/// Recomputes each account's balances from its journal entries and produces the
/// per-account audit line items a reconciliation run compares against storage.
/// </summary>
public interface IReconciliationService
{
    Task<IReadOnlyList<ReconciliationLineItem>> ComputeAsync(CancellationToken cancellationToken = default);
}

public interface IReconciliationReportRepository
{
    Task AddAsync(ReconciliationReport report, CancellationToken cancellationToken = default);
    Task<ReconciliationReport?> GetLatestAsync(CancellationToken cancellationToken = default);
    Task<List<ReconciliationReport>> ListAsync(int skip, int take, CancellationToken cancellationToken = default);
}