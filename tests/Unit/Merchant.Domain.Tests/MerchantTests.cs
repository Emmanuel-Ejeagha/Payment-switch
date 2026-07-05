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
        Assert.Empty(merchant.EnabledPaymentMethods);
        Assert.NotEmpty(merchant.DomainEvents);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantOnboardedEvent);
    }

    [Fact]
    public void Activate_WhenPending_ShouldBecomeActive()
    {
        var merchant = CreatePendingMerchant();

        merchant.Activate();

        Assert.Equal(MerchantStatus.Active, merchant.Status);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantActivatedEvent);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldThrow()
    {
        var merchant = CreatePendingMerchant();
        merchant.Activate();

        Assert.Throws<InvalidOperationException>(() => merchant.Activate());
    }

    [Fact]
    public void Suspend_WhenActive_ShouldBecomeSuspended()
    {
        var merchant = CreatePendingMerchant();
        merchant.Activate();
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
        var merchant = CreatePendingMerchant();
        merchant.Activate();
        merchant.ClearDomainEvents();

        merchant.UpdateConfiguration("https://acme.com/webhook", new List<string> { "card", "bank" });

        Assert.Equal("https://acme.com/webhook", merchant.WebhookUrl!.Value);
        Assert.Equal(2, merchant.EnabledPaymentMethods.Count);
        Assert.Contains(merchant.DomainEvents, e => e is MerchantConfigurationUpdatedEvent);
    }

    [Fact]
    public void UpdateConfiguration_WhenSuspended_ShouldThrow()
    {
        var merchant = CreatePendingMerchant();
        merchant.Activate();
        merchant.Suspend();

        Assert.Throws<InvalidOperationException>(() => merchant.UpdateConfiguration("https://hook.com", null));
    }

    private MerchantEntity CreatePendingMerchant() =>
        new(Guid.NewGuid(), new BusinessName("Test Co"), new MerchantEmail("test@test.com"));
}