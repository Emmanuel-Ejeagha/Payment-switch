using Merchant.Domain.DomainEvents;
using Merchant.Domain.ValueObjects;
using MerchantEntity = Merchant.Domain.Entities.Merchant;

namespace Merchant.Domain.Tests;

public class MerchantTests
{
    [Fact]
    public void Constructor_ShouldInitializeMerchantAsPending()
    {
        var id = Guid.NewGuid();
        var name = new BusinessName("Test Corp");
        var email = new MerchantEmail("test@example.com");

        var merchant = new MerchantEntity(id, name, email);

        Assert.Equal(id, merchant.Id);
        Assert.Equal(name, merchant.BusinessName);
        Assert.Equal(email, merchant.Email);
        Assert.Equal(MerchantStatus.Pending, merchant.Status);
        Assert.Null(merchant.WebhookUrl);
        Assert.NotNull(merchant.WebhookSecret);
        Assert.NotEmpty(merchant.WebhookSecret.Value);
        Assert.Empty(merchant.EnabledPaymentMethods);
        Assert.NotEmpty(merchant.DomainEvents);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantOnboardedEvent);
    }

    [Fact]
    public void Activate_WhenApproved_ShouldBecomeActive()
    {
        var merchant = CreateApprovedMerchant();

        merchant.Activate();

        Assert.Equal(MerchantStatus.Active, merchant.Status);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantActivatedEvent);
    }

    [Fact]
    public void Activate_WhenPending_ShouldThrow()
    {
        var merchant = CreatePendingMerchant();

        Assert.Throws<InvalidOperationException>(() => merchant.Activate());
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        var merchant = CreateApprovedMerchant();
        merchant.Activate();

        Assert.Throws<InvalidOperationException>(() => merchant.Activate());
    }

    [Fact]
    public void Approve_WhenPending_ShouldBecomeApproved()
    {
        var merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        merchant.Approve();

        Assert.Equal(MerchantStatus.Approved, merchant.Status);
        Assert.Null(merchant.RejectionReason);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantApprovedEvent);
    }

    [Fact]
    public void Approve_WhenNotPending_ShouldThrow()
    {
        var merchant = CreateApprovedMerchant();

        Assert.Throws<InvalidOperationException>(() => merchant.Approve());
    }

    [Fact]
    public void Reject_WhenPending_ShouldBecomeRejectedWithReason()
    {
        var merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        merchant.Reject("Missing business registration");

        Assert.Equal(MerchantStatus.Rejected, merchant.Status);
        Assert.Equal("Missing business registration", merchant.RejectionReason);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantRejectedEvent);
    }

    [Fact]
    public void Reject_WithoutReason_ShouldThrow()
    {
        var merchant = CreatePendingMerchant();

        Assert.Throws<ArgumentException>(() => merchant.Reject(null!));
        Assert.Throws<ArgumentException>(() => merchant.Reject("  "));
    }

    [Fact]
    public void Reject_WhenNotPending_ShouldThrow()
    {
        var merchant = CreateApprovedMerchant();

        Assert.Throws<InvalidOperationException>(() => merchant.Reject("late review"));
    }

    [Fact]
    public void Reactivate_WhenSuspended_ShouldBecomeActive()
    {
        var merchant = CreateActiveMerchant();
        merchant.Suspend();
        merchant.ClearDomainEvents();

        merchant.Reactivate();

        Assert.Equal(MerchantStatus.Active, merchant.Status);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantActivatedEvent);
    }

    [Fact]
    public void Reactivate_WhenNotSuspended_ShouldThrow()
    {
        var merchant = CreateActiveMerchant();

        Assert.Throws<InvalidOperationException>(() => merchant.Reactivate());
    }

    [Fact]
    public void Suspend_WhenActive_ShouldBecomeSuspended()
    {
        var merchant = CreateActiveMerchant();
        merchant.ClearDomainEvents();

        merchant.Suspend();

        Assert.Equal(MerchantStatus.Suspended, merchant.Status);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantSuspendedEvent);
    }

    [Fact]
    public void Suspend_WhenPending_ShouldThrow()
    {
        var merchant = CreatePendingMerchant();
        Assert.Throws<InvalidOperationException>(() => merchant.Suspend());
    }

    [Fact]
    public void UpdateConfiguration_WhenActive_ShouldUpdate()
    {
        var merchant = CreateActiveMerchant();
        merchant.ClearDomainEvents();

        merchant.UpdateConfiguration("https://acme.com/webhook", new List<string> { "card", "bank" });

        Assert.Equal("https://acme.com/webhook", merchant.WebhookUrl!.Value);
        Assert.Equal(2, merchant.EnabledPaymentMethods.Count);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantConfigurationUpdatedEvent);
    }

    [Fact]
    public void UpdateConfiguration_WhenSuspended_ShouldThrow()
    {
        var merchant = CreateActiveMerchant();
        merchant.Suspend();

        Assert.Throws<InvalidOperationException>(() => merchant.UpdateConfiguration("https://hook.com", null));
    }

    [Fact]
    public void UpdateSettlementInfo_ShouldPersistAndRaiseEvent()
    {
        var merchant = CreateActiveMerchant();
        merchant.ClearDomainEvents();

        merchant.UpdateSettlementInfo(new SettlementInfo("Acme Ltd", "0099887766", "Test Bank", "usd", "weekly"));

        Assert.NotNull(merchant.SettlementInfo);
        Assert.Equal("Acme Ltd", merchant.SettlementInfo!.BankAccountName);
        Assert.Equal("0099887766", merchant.SettlementInfo.BankAccountNumber);
        Assert.Equal("USD", merchant.SettlementInfo.SettlementCurrency);
        Assert.Equal("WEEKLY", merchant.SettlementInfo.SettlementSchedule);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantConfigurationUpdatedEvent);
    }

    [Fact]
    public void UpdateContactDetails_ShouldPersistAndRaiseEvent()
    {
        var merchant = CreateActiveMerchant();
        merchant.ClearDomainEvents();

        merchant.UpdateContactDetails(new ContactDetails("+2348000000000", "1 Test Street", "Ada"));

        Assert.NotNull(merchant.ContactDetails);
        Assert.Equal("+2348000000000", merchant.ContactDetails!.Phone);
        Assert.Equal("1 Test Street", merchant.ContactDetails.Address);
        Assert.Equal("Ada", merchant.ContactDetails.ContactPerson);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantConfigurationUpdatedEvent);
    }

    [Fact]
    public void UpdateContactDetails_WithBlankFields_ShouldStoreNulls()
    {
        var merchant = CreateActiveMerchant();

        merchant.UpdateContactDetails(new ContactDetails("  ", null, ""));

        Assert.NotNull(merchant.ContactDetails);
        Assert.Null(merchant.ContactDetails!.Phone);
        Assert.Null(merchant.ContactDetails.ContactPerson);
    }

    [Fact]
    public void RotateWebhookSecret_ShouldGenerateNewSecret()
    {
        var merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();
        var original = merchant.WebhookSecret!.Value;

        var rotated = merchant.RotateWebhookSecret();

        Assert.NotEqual(original, rotated.Value);
        Assert.Equal(rotated, merchant.WebhookSecret);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantConfigurationUpdatedEvent);
    }

    [Fact]
    public void RotateWebhookSecret_ShouldKeepPreviousSecretAndRecordRotationTime()
    {
        var merchant = CreatePendingMerchant();
        var original = merchant.WebhookSecret!.Value;
        Assert.Null(merchant.PreviousWebhookSecret);
        Assert.Null(merchant.WebhookSecretRotatedAtUtc);

        var before = DateTime.UtcNow;
        merchant.RotateWebhookSecret();
        var after = DateTime.UtcNow;

        Assert.Equal(original, merchant.PreviousWebhookSecret!.Value);
        Assert.NotNull(merchant.WebhookSecretRotatedAtUtc);
        Assert.InRange(merchant.WebhookSecretRotatedAtUtc.Value, before, after);
    }

    [Fact]
    public void RotateWebhookSecret_Twice_ShouldKeepMostRecentAsPrevious()
    {
        var merchant = CreatePendingMerchant();
        var first = merchant.WebhookSecret!.Value;

        merchant.RotateWebhookSecret();
        var second = merchant.WebhookSecret!.Value;
        merchant.RotateWebhookSecret();
        var third = merchant.WebhookSecret!.Value;

        Assert.Equal(second, merchant.PreviousWebhookSecret!.Value);
        Assert.Equal(third, merchant.WebhookSecret!.Value);
        Assert.NotEqual(first, second);
        Assert.NotEqual(second, third);
    }

    private MerchantEntity CreatePendingMerchant() =>
        new(Guid.NewGuid(), new BusinessName("Test Co"), new MerchantEmail("test@test.com"));

    private MerchantEntity CreateApprovedMerchant()
    {
        var merchant = CreatePendingMerchant();
        merchant.Approve();
        return merchant;
    }

    private MerchantEntity CreateActiveMerchant()
    {
        var merchant = CreateApprovedMerchant();
        merchant.Activate();
        return merchant;
    }
}