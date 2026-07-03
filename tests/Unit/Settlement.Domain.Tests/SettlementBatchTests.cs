using Settlement.Domain.DomainEvents;
using Settlement.Domain.Entities;
using Settlement.Domain.ValueObjects;

namespace Settlement.Domain.Tests;

public class SettlementBatchTests
{
    private readonly Guid _merchantId1 = Guid.NewGuid();
    private readonly Guid _merchantId2 = Guid.NewGuid();

    [Fact]
    public void Constructor_ShouldInitializeAsPending()
    {
        var batch = new SettlementBatch(Guid.NewGuid(), new DateTime(2026, 7, 1));

        Assert.Equal(SettlementStatus.Pending, batch.Status);
        Assert.Equal(0m, batch.TotalAmount);
        Assert.Empty(batch.Payouts);
        Assert.Contains(batch.DomainEvents, e => e is SettlementBatchCreatedEvent);
    }

    [Fact]
    public void AddPayout_ShouldAddAndRecalculateTotal()
    {
        var batch = CreatePendingBatch();
        var gross = new Money(100m, "USD");
        var fees = new Money(5m, "USD");

        batch.AddPayout(_merchantId1, gross, fees);

        Assert.Single(batch.Payouts);
        Assert.Equal(95m, batch.TotalAmount);
        Assert.Equal(95m, batch.Payouts[0].NetAmount.Amount);
    }

    [Fact]
    public void AddPayout_DuplicateMerchant_Throws()
    {
        var batch = CreatePendingBatch();
        batch.AddPayout(_merchantId1, new Money(100, "USD"), new Money(5, "USD"));

        Assert.Throws<InvalidOperationException>(() => batch.AddPayout(_merchantId1, new Money(50, "USD"), new Money(2, "USD")));
    }

    [Fact]
    public void AddPayout_CurrencyMismatch_Throws()
    {
        var batch = CreatePendingBatch();
        Assert.Throws<ArgumentException>(() => batch.AddPayout(_merchantId1, new Money(100, "USD"), new Money(5, "EUR")));
    }

    [Fact]
    public void AddPayout_AfterCompletion_Throws()
    {
        var batch = CreatePendingBatch();
        batch.AddPayout(_merchantId1, new Money(100, "USD"), new Money(5, "USD"));
        batch.Complete();

        Assert.Throws<InvalidOperationException>(() => batch.AddPayout(_merchantId2, new Money(50, "USD"), new Money(2, "USD")));
    }

    [Fact]
    public void Complete_ShouldSetStatusAndRaiseEvent()
    {
        var batch = CreatePendingBatch();
        batch.AddPayout(_merchantId1, new Money(100, "USD"), new Money(5, "USD"));
        batch.ClearDomainEvents();

        batch.Complete();

        Assert.Equal(SettlementStatus.Completed, batch.Status);
        Assert.NotNull(batch.CompletedAt);
        Assert.Contains(batch.DomainEvents, e => e is SettlementBatchCompletedEvent);
    }

    [Fact]
    public void Complete_AlreadyCompleted_Throws()
    {
        var batch = CreatePendingBatch();
        batch.AddPayout(_merchantId1, new Money(100, "USD"), new Money(5, "USD"));
        batch.Complete();

        Assert.Throws<InvalidOperationException>(() => batch.Complete());
    }

    private SettlementBatch CreatePendingBatch() =>
        new(Guid.NewGuid(), new DateTime(2026, 7, 1));
}