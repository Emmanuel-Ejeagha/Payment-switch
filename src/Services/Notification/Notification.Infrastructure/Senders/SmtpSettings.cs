namespace Notification.Infrastructure.Senders;

public class SmtpSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromAddress { get; set; } = "noreply@paymentswitch.com";
    public string FromName { get; set; } = "PaymentSwitch";
    public bool UseSsl { get; set; } = false;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
