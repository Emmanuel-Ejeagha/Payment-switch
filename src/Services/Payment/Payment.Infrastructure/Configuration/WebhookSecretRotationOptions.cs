namespace Payment.Infrastructure.Configuration;

/// <summary>
/// Length of the dual-secret grace window after a webhook-secret rotation.
/// During the window webhooks are still signed with the previous secret so the
/// merchant's verifier keeps working until it adopts the new one (TASK-006).
/// </summary>
public class WebhookSecretRotationOptions
{
    public const string SectionName = "WebhookSecretRotation";

    public int GracePeriodHours { get; set; } = 72;
}
