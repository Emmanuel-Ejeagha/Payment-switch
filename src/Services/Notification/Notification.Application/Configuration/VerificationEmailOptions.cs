namespace Notification.Application.Configuration;

public class VerificationEmailOptions
{
    /// <summary>Base URL of the frontend that hosts the /verify-email page.</summary>
    public string FrontendBaseUrl { get; set; } = "";

    public string Subject { get; set; } = "Confirm your email address";
}
