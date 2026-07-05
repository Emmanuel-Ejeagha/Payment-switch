using BuildingBlocks.Shared.Aggregate;
using Merchant.Domain.DomainEvents;
using Merchant.Domain.ValueObjects;

namespace Merchant.Domain.Entities;

public class Merchant : AggregateRoot
{
    public BusinessName BusinessName { get; private set; } = default!;
    public MerchantEmail Email { get; private set; } = default!;
    public MerchantStatus Status { get; private set; } = default!;
    public WebhookUrl? WebhookUrl { get; private set; } = default!;
    public IReadOnlyList<string> EnabledPaymentMethods => _paymentMethods.AsReadOnly();
    private readonly List<string> _paymentMethods = new();
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Merchant() : base() { } 

    public Merchant(Guid id, BusinessName businessName, MerchantEmail email) : base(id)
    {
        BusinessName = businessName ?? throw new ArgumentNullException(nameof(businessName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Status = MerchantStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantOnboardedEvent(Id, businessName.Value, email.Value));
    }

    public void Activate()
    {
        if (Status != MerchantStatus.Pending)
            throw new InvalidOperationException("Only pending merchants can be activated.");
        Status = MerchantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantActivatedEvent(Id));
    }

    public void Suspend()
    {
        if (Status != MerchantStatus.Active)
            throw new InvalidOperationException("Only active merchants can be suspended.");
        Status = MerchantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantSuspendedEvent(Id));
    }

    public void UpdateConfiguration(string? webhookUrl, List<string>? paymentMethods)
    {
        if (Status != MerchantStatus.Active)
            throw new InvalidOperationException("Cannot update configuration: merchant is not active.");

        if (webhookUrl is not null)
            WebhookUrl = new WebhookUrl(webhookUrl);

        if (paymentMethods is not null)
        {
            _paymentMethods.Clear();
            _paymentMethods.AddRange(paymentMethods);
        }

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantConfigurationUpdatedEvent(Id));
    }
}