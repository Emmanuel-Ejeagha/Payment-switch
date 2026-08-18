using BuildingBlocks.Shared.Results;
using LedgerAppDbContext = Ledger.Infrastructure.Persistence.AppDbContext;
using Ledger.Infrastructure.Queries;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.DTOs;
using Settlement.Application.Interfaces;

namespace E2E.IntegrationTests.Support;

/// <summary>
/// In-memory stand-in for the Settlement API's gRPC ledger lookup. Runs the
/// Ledger service's own <see cref="DailyPayoutQuery"/> against the Ledger
/// database so settlement totals reflect the real journal postings.
/// </summary>
public sealed class DbBackedLedgerService : ILedgerService
{
    private readonly DbContextOptions<LedgerAppDbContext> _options;

    public DbBackedLedgerService(string connectionString)
    {
        _options = new DbContextOptionsBuilder<LedgerAppDbContext>()
            .UseNpgsql(connectionString)
            .Options;
    }

    public async Task<Result<List<MerchantPayoutData>>> GetDailyPayoutDataAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await using var db = new LedgerAppDbContext(_options);
        var query = new DailyPayoutQuery(db);
        var rows = await query.GetAsync(date, cancellationToken);

        return Result<List<MerchantPayoutData>>.Success(
            rows.Select(r => new MerchantPayoutData(r.MerchantId, r.GrossVolume, r.Fees, r.Currency)).ToList());
    }
}