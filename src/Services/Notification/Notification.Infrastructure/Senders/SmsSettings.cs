namespace Notification.Infrastructure.Senders;

/// <summary>
/// Configuration for the SMS channel. No provider is wired yet, so the channel
/// is explicitly disabled unless <c>Enabled</c> is set. When disabled the sender
/// logs a clear warning instead of pretending to deliver.
/// </summary>
public class SmsSettings
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "None";
}
