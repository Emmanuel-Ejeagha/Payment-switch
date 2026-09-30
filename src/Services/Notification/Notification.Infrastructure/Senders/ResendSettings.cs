namespace Notification.Infrastructure.Senders;

public class ResendSettings
{
    /// <summary>Resend API token (secret). Required in Production.</summary>
    public string ApiKey { get; set; } = "";

    public string FromEmail { get; set; } = "noreply@paymentswitch.com";
    public string FromName { get; set; } = "PaymentSwitch";

    /// <summary>HTTP timeout for the Resend API, in seconds.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}
