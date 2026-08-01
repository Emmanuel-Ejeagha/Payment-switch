using BuildingBlocks.Shared.Aggregate;
using Merchant.Domain.DomainEvents;
using Merchant.Domain.ValueObjects;

namespace Merchant.Domain.Entities;

public class Merchant : AggregateRoot
{
    public Guid? OwnerId { get; private set; }
    public BusinessName BusinessName { get; private set; } = default!;
    public MerchantEmail Email { get; private set; } = default!;
    public MerchantStatus Status { get; private set; } = default!;
    public WebhookUrl? WebhookUrl { get; private set; } = default!;
    public IReadOnlyList<string> EnabledPaymentMethods => _paymentMethods.AsReadOnly();
    private readonly List<string> _paymentMethods = new();
    public IReadOnlyList<MerchantApiKey> ApiKeys => _apiKeys.AsReadOnly();
    private readonly List<MerchantApiKey> _apiKeys = new();
    public bool AutoCapture { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Merchant() : base() { } 

    public Merchant(Guid id, Guid? ownerId, BusinessName businessName, MerchantEmail email) : base(id)
    {
        OwnerId = ownerId;
        BusinessName = businessName ?? throw new ArgumentNullException(nameof(businessName));
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Status = MerchantStatus.Pending;
        AutoCapture = true;
        CreatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantOnboardedEvent(Id, businessName.Value, email.Value));
    }

    public Merchant(Guid id, BusinessName businessName, MerchantEmail email)
        : this(id, null, businessName, email) { }

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

    public void UpdateConfiguration(string? webhookUrl, List<string>? paymentMethods, bool? autoCapture = null)
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

        if (autoCapture.HasValue)
            AutoCapture = autoCapture.Value;

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantConfigurationUpdatedEvent(Id));
    }

    public MerchantApiKey GenerateApiKey(string keyHash, string keyPrefix, string environment)
    {
        var apiKey = new MerchantApiKey(Guid.NewGuid(), Id, keyHash, keyPrefix, environment);
        _apiKeys.Add(apiKey);
        UpdatedAt = DateTime.UtcNow;
        return apiKey;
    }

    public void RevokeApiKey(Guid keyId)
    {
        var key = _apiKeys.FirstOrDefault(k => k.Id == keyId);
        if (key is null)
            throw new InvalidOperationException("API key not found.");
        key.Revoke();
        UpdatedAt = DateTime.UtcNow;
    }
}