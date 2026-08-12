using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// Proves the ledger's DB-level integrity invariants on a real Postgres
/// (TASK-015): unique (MerchantId, Currency), non-negative balances, and the
/// journal FK on DELETE RESTRICT so the audit trail is never cascaded away.
/// </summary>
public class LedgerIntegrityConstraintTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public LedgerIntegrityConstraintTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DuplicateAccountPerMerchantCurrency_IsRejectedByUniqueIndex()
    {
        var merchantId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.LedgerAccounts.Add(new LedgerAccount(Guid.NewGuid(), merchantId, "USD"));
        await db.SaveChangesAsync();

        // Concurrent auto-create must not produce a second (merchant, currency)
        // account: the unique index rejects the insert.
        db.LedgerAccounts.Add(new LedgerAccount(Guid.NewGuid(), merchantId, "USD"));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());

        var postgres = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, postgres.SqlState);
    }

    [Fact]
    public async Task NegativeBalance_IsRejectedByCheckConstraint()
    {
        var merchantId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = new LedgerAccount(Guid.NewGuid(), merchantId, "USD");
        db.LedgerAccounts.Add(account);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync(
                "UPDATE \"LedgerAccounts\" SET \"AvailableBalance\" = -100 WHERE \"Id\" = {0}", account.Id));

        Assert.Equal(PostgresErrorCodes.CheckViolation, ex.SqlState);
    }

    [Fact]
    public async Task AccountWithJournalEntries_CannotBeDeleted_WithoutCascadingAuditTrail()
    {
        var merchantId = Guid.NewGuid();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var account = new LedgerAccount(Guid.NewGuid(), merchantId, "USD");
        account.ReserveFunds(new Money(5000, "USD"), new CorrelationId($"reserve-{Guid.NewGuid():N}"));
        db.LedgerAccounts.Add(account);
        await db.SaveChangesAsync();

        // Direct DELETE proves the DB itself enforces RESTRICT — the audit trail
        // must survive any attempt to remove the account.
        var ex = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("DELETE FROM \"LedgerAccounts\" WHERE \"Id\" = {0}", account.Id));

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, ex.SqlState);
    }
}