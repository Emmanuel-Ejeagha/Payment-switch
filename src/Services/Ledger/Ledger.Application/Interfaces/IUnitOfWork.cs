namespace Ledger.Application.Interfaces;

public interface IUnitOfWork
{
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Detaches all tracked entities so a rolled-back scope can safely
    /// re-read fresh state (e.g. converging on a concurrently inserted row).
    /// </summary>
    void ClearTrackedEntities();
}
