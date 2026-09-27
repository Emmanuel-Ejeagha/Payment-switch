using BuildingBlocks.Shared.Messaging;

namespace BuildingBlocks.Shared.Tests;

public class PaymentEventChannelsTests
{
    [Fact]
    public void PublishedPaymentEvents_EveryEventHasInboxCoverage()
    {
        var uncovered = PaymentEventChannels.PublishedPaymentEvents
            .Where(e => !PaymentEventChannels.HasInboxCoverage(e))
            .ToList();

        Assert.Empty(uncovered);
    }

    [Fact]
    public void PublishedPaymentEvents_IsSubsetOfLedgerPlusNotificationBindings()
    {
        var allBound = new HashSet<string>(PaymentEventChannels.LedgerBoundPaymentEvents);
        allBound.UnionWith(PaymentEventChannels.NotificationBoundPaymentEvents);

        var uncovered = PaymentEventChannels.PublishedPaymentEvents
            .Where(e => !allBound.Contains(e))
            .ToList();

        Assert.Empty(uncovered);
    }

    [Fact]
    public void LedgerBindings_AreSubsetOfPublished()
    {
        // Ledger should not bind an event that Payment never publishes — that
        // would be dead code, and it would hint at a missing publish.
        var extra = PaymentEventChannels.LedgerBoundPaymentEvents
            .Where(e => !PaymentEventChannels.PublishedPaymentEvents.Contains(e))
            .ToList();

        Assert.Empty(extra);
    }

    [Fact]
    public void NotificationBindings_AreSubsetOfPublished()
    {
        var extra = PaymentEventChannels.NotificationBoundPaymentEvents
            .Where(e => !PaymentEventChannels.PublishedPaymentEvents.Contains(e))
            .ToList();

        Assert.Empty(extra);
    }

    [Fact]
    public void PublishedPaymentEvents_HasExpectedCardinality()
    {
        // Guard against accidental add/remove without updating the registry test.
        Assert.Equal(7, PaymentEventChannels.PublishedPaymentEvents.Count);
    }
}
