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
    public WebhookSecret? WebhookSecret { get; private set; } = default!;
    public WebhookSecret? PreviousWebhookSecret { get; private set; }
    public DateTime? WebhookSecretRotatedAtUtc { get; private set; }
    public IReadOnlyList<string> EnabledPaymentMethods => _paymentMethods.AsReadOnly();
    private readonly List<string> _paymentMethods = new();
    public IReadOnlyList<MerchantApiKey> ApiKeys => _apiKeys.AsReadOnly();
    private readonly List<MerchantApiKey> _apiKeys = new();
    public bool AutoCapture { get; private set; } = true;
    public string? RejectionReason { get; private set; }
    public SettlementInfo? SettlementInfo { get; private set; }
    public ContactDetails? ContactDetails { get; private set; }
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
        WebhookSecret = WebhookSecret.Generate();
        CreatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantOnboardedEvent(Id, businessName.Value, email.Value));
    }

    public Merchant(Guid id, BusinessName businessName, MerchantEmail email)
        : this(id, null, businessName, email) { }

    public void Approve()
    {
        if (Status != MerchantStatus.Pending)
            throw new InvalidOperationException("Only pending merchants can be approved.");
        Status = MerchantStatus.Approved;
        RejectionReason = null;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantApprovedEvent(Id));
    }

    public void Reject(string reason)
    {
        if (Status != MerchantStatus.Pending)
            throw new InvalidOperationException("Only pending merchants can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        Status = MerchantStatus.Rejected;
        RejectionReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantRejectedEvent(Id, RejectionReason));
    }

    public void Activate()
    {
        if (Status != MerchantStatus.Approved)
            throw new InvalidOperationException("Only approved merchants can be activated.");
        Status = MerchantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantActivatedEvent(Id));
    }

    public void Reactivate()
    {
        if (Status != MerchantStatus.Suspended)
            throw new InvalidOperationException("Only suspended merchants can be reactivated.");
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

        WebhookSecret ??= WebhookSecret.Generate();

        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantConfigurationUpdatedEvent(Id));
    }

    public WebhookSecret RotateWebhookSecret()
    {
        PreviousWebhookSecret = WebhookSecret;
        WebhookSecret = WebhookSecret.Generate();
        WebhookSecretRotatedAtUtc = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantConfigurationUpdatedEvent(Id));
        return WebhookSecret;
    }

    public void UpdateSettlementInfo(SettlementInfo settlementInfo)
    {
        SettlementInfo = settlementInfo ?? throw new ArgumentNullException(nameof(settlementInfo));
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new MerchantConfigurationUpdatedEvent(Id));
    }

    public void UpdateContactDetails(ContactDetails contactDetails)
    {
        ContactDetails = contactDetails ?? throw new ArgumentNullException(nameof(contactDetails));
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