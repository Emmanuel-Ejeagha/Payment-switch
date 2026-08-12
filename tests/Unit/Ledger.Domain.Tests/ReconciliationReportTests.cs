using Ledger.Domain.Entities;

namespace Ledger.Domain.Tests;

public class ReconciliationReportTests
{
    [Fact]
    public void Report_WithAllMatchingItems_IsCompleted()
    {
        var report = new ReconciliationReport(Guid.NewGuid(), new[]
        {
            new ReconciliationLineItem(Guid.NewGuid(), "USD", 5000, 5000, 0, 0, 0, 0),
            new ReconciliationLineItem(Guid.NewGuid(), "EUR", 100, 100, 300, 300, 300, 300)
        }, DateTime.UtcNow);

        Assert.Equal(ReconciliationStatus.Completed, report.Status);
        Assert.Equal(0, report.MismatchCount);
        Assert.Equal(2, report.TotalAccounts);
    }

    [Fact]
    public void Report_WithMismatchedItem_IsMismatchFound()
    {
        var report = new ReconciliationReport(Guid.NewGuid(), new[]
        {
            new ReconciliationLineItem(Guid.NewGuid(), "USD", 5000, 5000, 0, 0, 0, 0),
            new ReconciliationLineItem(Guid.NewGuid(), "USD", 5000, 4998, 0, 0, 0, 0)
        }, DateTime.UtcNow);

        Assert.Equal(ReconciliationStatus.MismatchFound, report.Status);
        Assert.Equal(1, report.MismatchCount);
    }

    [Fact]
    public void LineItem_WithEqualBalances_IsMatch()
    {
        var item = new ReconciliationLineItem(Guid.NewGuid(), "USD", 100, 100, 200, 200, 200, 200);
        Assert.True(item.IsMatch);
    }

    [Fact]
    public void LineItem_WithAnyBalanceDifference_IsNotMatch()
    {
        var item = new ReconciliationLineItem(Guid.NewGuid(), "USD", 100, 99, 200, 200, 200, 200);
        Assert.False(item.IsMatch);
    }
}