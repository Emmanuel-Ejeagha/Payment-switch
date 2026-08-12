using Identity.Application.Exceptions;
using Identity.Application.Interfaces;
using Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Identity.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsEmailUniqueViolation(ex))
        {
            // Backstop for the application-level duplicate check (TASK-014): a
            // concurrent registration or a case-variant email that raced past the
            // existence check hits the unique index here. Translate it into a
            // domain result instead of leaking a 500 to the client.
            throw new EmailConflictException("The email address is already registered.", ex);
        }
    }

    private static bool IsEmailUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            && ex.Entries.Any(e => e.Entity is User);
    }
}