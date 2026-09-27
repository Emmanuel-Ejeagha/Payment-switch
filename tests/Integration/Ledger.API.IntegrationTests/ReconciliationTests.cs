using Ledger.Application.Features.Commands.RunReconciliation;
using Ledger.Domain.Entities;
using Ledger.Domain.ValueObjects;
using Ledger.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ledger.API.IntegrationTests;

/// <summary>
/// TASK-017: reconciliation verifies each account's stored balances against its
/// journal. A clean run passes; a manually-corrupted balance (the injected
/// mismatch) is detected and surfaced in the report history.
/// </summary>
public class ReconciliationTests : IClassFixture<LedgerApiFactory>
{
    private readonly LedgerApiFactory _factory;

    public ReconciliationTests(LedgerApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Run_WithConsistentJournal_ProducesCompletedReport()
    {
        var merchantId = Guid.NewGuid();

        SeedAccount(merchantId, operations: a =>
        {
            a.ReserveFunds(new Money(5000, "USD"), new CorrelationId("rec-reserve"));
            a.CaptureFunds(new Money(5000, "USD"), new CorrelationId("rec-capture"));
            a.ChargeFees(new Money(150, "USD"), new CorrelationId("rec-fees"));
        });

        var report = await RunAsync();

        Assert.Equal(ReconciliationStatus.Completed, report.Status);
        Assert.Equal(0, report.MismatchCount);
        var item = report.Items.Single(i => i.MerchantId == merchantId);
        Assert.True(item.IsMatch);
        Assert.Equal(4850, item.ExpectedAvailable);
        Assert.Equal(4850, item.ActualAvailable);
    }

    [Fact]
    public async Task Run_DetectsInjectedBalanceMismatch()
    {
        var merchantId = Guid.NewGuid();

        SeedAccount(merchantId, operations: a =>
        {
            a.ReserveFunds(new Money(5000, "USD"), new CorrelationId("rec-reserve-2"));
            a.CaptureFunds(new Money(5000, "USD"), new CorrelationId("rec-capture-2"));
        });

        // A clean run first, so the report history has a passing baseline.
        var clean = await RunAsync();
        Assert.Equal(ReconciliationStatus.Completed, clean.Status);

        // Inject the corruption: drift the stored available balance away from
        // what the journal entries can explain (simulates a lost/duplicate post).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"LedgerAccounts\" SET \"AvailableBalance\" = \"AvailableBalance\" - 1 WHERE \"MerchantId\" = {0}",
                merchantId);
        }

        var mismatched = await RunAsync();

        Assert.Equal(ReconciliationStatus.MismatchFound, mismatched.Status);
        Assert.Equal(1, mismatched.MismatchCount);
        var item = mismatched.Items.Single(i => i.MerchantId == merchantId);
        Assert.False(item.IsMatch);
        Assert.Equal(5000, item.ExpectedAvailable);
        Assert.Equal(4999, item.ActualAvailable);

        // Restore the balance so the injected mismatch does not pollute the rest
        // of the run (all tests in this class share one database).
        using (var restore = _factory.Services.CreateScope())
        {
            var db = restore.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE \"LedgerAccounts\" SET \"AvailableBalance\" = \"AvailableBalance\" + 1 WHERE \"MerchantId\" = {0}",
                merchantId);
        }
    }

    [Fact]
    public async Task Latest_ReturnsMostRecentReport_FromHistory()
    {
        var merchantId = Guid.NewGuid();
        SeedAccount(merchantId, operations: a =>
        {
            a.ReserveFunds(new Money(1000, "USD"), new CorrelationId("rec-reserve-3"));
            a.CaptureFunds(new Money(1000, "USD"), new CorrelationId("rec-capture-3"));
        });

        var report = await RunAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var saved = await db.ReconciliationReports
            .Include(r => r.Items)
            .FirstAsync(r => r.Id == report.Id);

        Assert.Equal(ReconciliationStatus.Completed, saved.Status);
        Assert.True(saved.TotalAccounts >= 1);
        Assert.Contains(saved.Items, i => i.MerchantId == merchantId && i.IsMatch);
    }

    private void SeedAccount(Guid merchantId, Action<LedgerAccount> operations)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var account = new LedgerAccount(Guid.NewGuid(), merchantId, "USD");
        operations(account);
        db.LedgerAccounts.Add(account);
        db.SaveChanges();
    }

    private async Task<Ledger.Application.DTOs.ReconciliationReportDto> RunAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<RunReconciliationHandler>();
        var result = await handler.Handle(new RunReconciliationCommand());
        Assert.True(result.IsSuccess);
        return result.Value;
    }
}