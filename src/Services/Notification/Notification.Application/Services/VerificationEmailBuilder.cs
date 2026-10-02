namespace Notification.Application.Services;

/// <summary>
/// Rendered verification email: the link carries only the token issued by
/// Identity (never keys or secrets). The text body always contains the raw
/// link as a fallback for clients that do not render the HTML button.
/// </summary>
public record VerificationEmail(
    string Recipient,
    string Subject,
    string TextBody,
    string Link,
    string ExpiryLabel);

/// <summary>
/// Builds the verification email content. The link is rooted at the configured,
/// trusted frontend origin — never at request Host headers.
/// </summary>
public static class VerificationEmailBuilder
{
    public static VerificationEmail Build(
        string recipient,
        string token,
        DateTime issuedAtUtc,
        DateTime expiresAtUtc,
        string subject,
        string frontendBaseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipient);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var baseUrl = frontendBaseUrl.TrimEnd('/');
        var link = string.IsNullOrWhiteSpace(baseUrl)
            ? string.Empty
            : $"{baseUrl}/verify-email?email={Uri.EscapeDataString(recipient)}&token={Uri.EscapeDataString(token)}";

        var expiryHours = Math.Max(1, (int)Math.Round((expiresAtUtc - issuedAtUtc).TotalHours));
        var expiryLabel = expiryHours == 1 ? "1 hour" : $"{expiryHours} hours";

        var textBody = string.IsNullOrEmpty(link)
            ? "Confirm your email address.\n\n" +
              $"Your verification code is: {token}\n\n" +
              $"It expires in {expiryLabel}.\n" +
              "If you did not register for PaymentSwitch, you can ignore this email."
            : "Confirm your email address.\n\n" +
              $"Open the link below to verify your email:\n{link}\n\n" +
              $"If the link does not open, copy and paste it into your browser. It expires in {expiryLabel}.\n" +
              "If you did not register for PaymentSwitch, you can ignore this email.";

        return new VerificationEmail(recipient, subject, textBody, link, expiryLabel);
    }
}
