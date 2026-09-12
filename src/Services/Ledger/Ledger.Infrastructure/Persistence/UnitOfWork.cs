using BuildingBlocks.Shared.Exceptions;
using Ledger.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Ledger.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction ??= await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                $"A concurrency conflict occurred while saving. {ex.Message}");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Backstop for check-then-insert races (e.g. two concurrent first
            // payments auto-creating the same ledger account): surface it as a
            // domain result instead of leaking a 500 to the client.
            throw new UniqueConstraintViolationException(
                $"A uniqueness constraint was violated while saving. {ex.Message}");
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void ClearTrackedEntities() => _context.ChangeTracker.Clear();

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
        || ex.InnerException?.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
