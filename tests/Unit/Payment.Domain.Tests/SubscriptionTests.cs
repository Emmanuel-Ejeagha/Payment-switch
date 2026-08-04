using Payment.Domain.Entities;
using Payment.Domain.ValueObjects;

namespace Payment.Domain.Tests;

public class BillingIntervalTests
{
    [Theory]
    [InlineData("day")]
    [InlineData("week")]
    [InlineData("month")]
    [InlineData("year")]
    public void Constructor_ShouldAcceptSupportedUnits(string unit)
    {
        var interval = new BillingInterval(unit);

        Assert.Equal(unit, interval.Unit);
        Assert.Equal(1, interval.Count);
    }

    [Fact]
    public void Constructor_ShouldNormalizeUnitCasing()
    {
        Assert.Equal("month", new BillingInterval("MONTH").Unit);
    }

    [Fact]
    public void Constructor_ShouldRejectUnknownUnit()
    {
        Assert.Throws<ArgumentException>(() => new BillingInterval("fortnight"));
    }

    [Fact]
    public void Constructor_ShouldRejectNonPositiveCount()
    {
        Assert.Throws<ArgumentException>(() => new BillingInterval("month", 0));
    }

    [Fact]
    public void AddTo_ShouldAdvanceByUnitAndCount()
    {
        var start = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        Assert.Equal(start.AddDays(5), new BillingInterval("day", 5).AddTo(start));
        Assert.Equal(start.AddDays(14), new BillingInterval("week", 2).AddTo(start));
        Assert.Equal(start.AddMonths(1), new BillingInterval("month").AddTo(start));
        Assert.Equal(start.AddYears(1), new BillingInterval("year").AddTo(start));
    }

    [Fact]
    public void Equality_ShouldBeByValue()
    {
        Assert.Equal(new BillingInterval("month", 3), new BillingInterval("month", 3));
        Assert.NotEqual(new BillingInterval("month", 3), new BillingInterval("month", 1));
    }
}

public class PlanTests
{
    private static Plan CreatePlan(long amount = 500_00) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Plan.NewCode(),
        "Pro monthly",
        new Money(amount, "NGN"),
        new BillingInterval("month"));

    [Fact]
    public void NewCode_ShouldUsePlanPrefix()
    {
        Assert.StartsWith("plan_", Plan.NewCode());
    }

    [Fact]
    public void Constructor_ShouldStartActive()
    {
        Assert.True(CreatePlan().Active);
    }

    [Fact]
    public void Constructor_ShouldRejectZeroAmount()
    {
        Assert.Throws<ArgumentException>(() => CreatePlan(0));
    }

    [Fact]
    public void Archive_ShouldDeactivateAndBeIdempotent()
    {
        var plan = CreatePlan();

        plan.Archive();
        Assert.False(plan.Active);
        var stamp = plan.UpdatedAt;

        plan.Archive();
        Assert.Equal(stamp, plan.UpdatedAt);
    }
}

public class SubscriptionTests
{
    private static readonly BillingInterval Monthly = new("month");

    private static Subscription CreateSubscription(DateTime? startAt = null) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Subscription.NewCode(),
        "card_abc123",
        Monthly,
        startAt);

    [Fact]
    public void NewCode_ShouldUseSubscriptionPrefix()
    {
        Assert.StartsWith("sub_", Subscription.NewCode());
    }

    [Fact]
    public void Constructor_ShouldStartIncompleteAndBillImmediately()
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = CreateSubscription(start);

        Assert.Equal(SubscriptionStatus.Incomplete, subscription.Status);
        Assert.Equal(start, subscription.CurrentPeriodStart);
        Assert.Equal(start.AddMonths(1), subscription.CurrentPeriodEnd);
        Assert.Equal(start, subscription.NextBillingAt);
    }

    [Fact]
    public void Constructor_ShouldRequireCardToken()
    {
        Assert.Throws<ArgumentException>(() => new Subscription(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Subscription.NewCode(), "  ", Monthly));
    }

    [Fact]
    public void MarkCyclePaid_ShouldRollPeriodForwardAndActivate()
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = CreateSubscription(start);

        subscription.MarkCyclePaid(Monthly);

        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
        Assert.Equal(start.AddMonths(1), subscription.CurrentPeriodStart);
        Assert.Equal(start.AddMonths(2), subscription.CurrentPeriodEnd);
        Assert.Equal(start.AddMonths(1), subscription.NextBillingAt);
    }

    [Fact]
    public void MarkCyclePaid_ShouldCancelWhenCancelAtPeriodEndRequested()
    {
        var subscription = CreateSubscription();
        subscription.CancelAtEndOfPeriod();

        subscription.MarkCyclePaid(Monthly);

        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
        Assert.Null(subscription.NextBillingAt);
    }

    [Fact]
    public void MarkPastDue_ShouldScheduleRetry()
    {
        var subscription = CreateSubscription();
        var retryAt = DateTime.UtcNow.AddHours(1);

        subscription.MarkPastDue(retryAt);

        Assert.Equal(SubscriptionStatus.PastDue, subscription.Status);
        Assert.Equal(retryAt, subscription.NextBillingAt);
    }

    [Fact]
    public void Cancel_ShouldClearNextBillingAndBeIdempotent()
    {
        var subscription = CreateSubscription();

        subscription.Cancel();
        Assert.Equal(SubscriptionStatus.Canceled, subscription.Status);
        Assert.Null(subscription.NextBillingAt);
        var canceledAt = subscription.CanceledAt;

        subscription.Cancel();
        Assert.Equal(canceledAt, subscription.CanceledAt);
    }

    [Fact]
    public void MarkCyclePaid_ShouldThrowOnceCanceled()
    {
        var subscription = CreateSubscription();
        subscription.Cancel();

        Assert.Throws<InvalidOperationException>(() => subscription.MarkCyclePaid(Monthly));
    }

    [Fact]
    public void UpdateCardToken_ShouldReplaceToken()
    {
        var subscription = CreateSubscription();

        subscription.UpdateCardToken("card_new456");

        Assert.Equal("card_new456", subscription.CardToken);
    }
}

public class InvoiceTests
{
    private static Invoice CreateInvoice(long amount = 500_00) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Invoice.NewCode(),
        new Money(amount, "NGN"),
        new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void NewCode_ShouldUseInvoicePrefix()
    {
        Assert.StartsWith("inv_", Invoice.NewCode());
    }

    [Fact]
    public void Constructor_ShouldStartOpenAndRaiseIssuedEvent()
    {
        var invoice = CreateInvoice();

        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(0, invoice.AttemptCount);
        Assert.Single(invoice.DomainEvents);
    }

    [Fact]
    public void Constructor_ShouldRejectInvertedPeriod()
    {
        Assert.Throws<ArgumentException>(() => new Invoice(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Invoice.NewCode(), new Money(100, "NGN"),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void MarkPaid_ShouldRecordIntentAndClearLastError()
    {
        var invoice = CreateInvoice();
        var intentId = Guid.NewGuid();
        invoice.RecordFailedAttempt("declined");

        invoice.MarkPaid(intentId);

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(intentId, invoice.PaymentIntentId);
        Assert.Null(invoice.LastError);
        Assert.NotNull(invoice.PaidAt);
        Assert.Equal(2, invoice.AttemptCount);
    }

    [Fact]
    public void MarkPaid_ShouldBeIdempotent()
    {
        var invoice = CreateInvoice();
        var intentId = Guid.NewGuid();

        invoice.MarkPaid(intentId);
        invoice.MarkPaid(Guid.NewGuid());

        Assert.Equal(intentId, invoice.PaymentIntentId);
        Assert.Equal(1, invoice.AttemptCount);
    }

    [Fact]
    public void RecordFailedAttempt_ShouldKeepInvoiceOpenForRetry()
    {
        var invoice = CreateInvoice();

        invoice.RecordFailedAttempt("insufficient funds");

        Assert.Equal(InvoiceStatus.Open, invoice.Status);
        Assert.Equal(1, invoice.AttemptCount);
        Assert.Equal("insufficient funds", invoice.LastError);
    }

    [Fact]
    public void MarkUncollectible_ShouldCloseInvoice()
    {
        var invoice = CreateInvoice();

        invoice.MarkUncollectible();

        Assert.Equal(InvoiceStatus.Uncollectible, invoice.Status);
    }

    [Fact]
    public void Void_ShouldRejectPaidInvoice()
    {
        var invoice = CreateInvoice();
        invoice.MarkPaid(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => invoice.Void());
    }
}
