using BuildingBlocks.Shared.Aggregate;
using Notification.Domain.DomainEvents;
using Notification.Domain.ValueObjects;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Notification.Domain.Tests")]

namespace Notification.Domain.Entities;

public class Notification : AggregateRoot
{
    public string Recipient { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; } = default!;
    public string? Subject { get; private set; }
    public string? Body { get; private set; }
    public string? WebhookUrl { get; private set; }
    public string Payload { get; private set; } = default!;
    public NotificationStatus Status { get; internal set; } = default!;
    public int RetryCount { get; internal set; }
    public int MaxRetries { get; private set; }
    public DateTime? NextRetryAt { get; internal set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    private Notification() : base() { }

    public Notification(
        Guid id,
        string recipient,
        NotificationChannel channel,
        string? subject,
        string? body,
        string? webhookUrl,
        string payload,
        int maxRetries = 5) : base(id)
    {
        if (string.IsNullOrWhiteSpace(recipient))
            throw new ArgumentException("Recipient cannot be empty.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentException("Payload cannot be empty.", nameof(payload));

        Recipient = recipient;
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Subject = subject;
        Body = body;
        WebhookUrl = webhookUrl;
        Payload = payload;
        Status = NotificationStatus.Pending;
        MaxRetries = maxRetries;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsSent()
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Notification has already been sent.");

        Status = NotificationStatus.Sent;
        ProcessedAt = DateTime.UtcNow;
        NextRetryAt = null;
        AddDomainEvent(new NotificationSentEvent(Id, Recipient, Channel.Value));
    }

    public void MarkAsFailed()
    {
        if (Status == NotificationStatus.Sent)
            throw new InvalidOperationException("Sent notification cannot fail.");

        RetryCount++;
        if (RetryCount >= MaxRetries)
        {
            Status = NotificationStatus.Failed;
            NextRetryAt = null;
            ProcessedAt = DateTime.UtcNow;
            AddDomainEvent(new NotificationPermanentlyFailedEvent(Id, Recipient, Channel.Value));
        }
        else
        {
            var delayMinutes = Math.Pow(2, RetryCount);
            NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            Status = NotificationStatus.Pending;
        }
    }
}