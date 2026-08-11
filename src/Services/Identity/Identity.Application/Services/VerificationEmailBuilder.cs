using BuildingBlocks.Shared.Email;

namespace Identity.Application.Services;

/// <summary>
/// Builds the verification email, embedding the single-use token in a link to
/// the frontend /verify-email page. When no frontend base URL is configured the
/// token is embedded in the body so the flow still works in development.
/// </summary>
public static class VerificationEmailBuilder
{
    public static EmailMessage Build(
        string recipient,
        string plainToken,
        string subject,
        string frontendBaseUrl)
    {
        var baseUrl = frontendBaseUrl.TrimEnd('/');
        var email = Uri.EscapeDataString(recipient);

        string textBody;
        string? htmlBody = null;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            textBody = "Confirm your email address.\n\n" +
                       $"Your verification token is: {plainToken}\n\n" +
                       "It expires in 24 hours.";
        }
        else
        {
            var link = $"{baseUrl}/verify-email?email={email}&token={Uri.EscapeDataString(plainToken)}";
            textBody = "Confirm your email address.\n\n" +
                       $"Open the link below to verify your email:\n{link}\n\n" +
                       "If the link does not open, copy and paste it into your browser. It expires in 24 hours.";
            htmlBody = $"<h2>Confirm your email address</h2>" +
                       $"<p>Hi,</p>" +
                       $"<p>Please confirm your email address by clicking the button below:</p>" +
                       $"<p style=\"margin:24px 0\"><a href=\"{link}\" style=\"background:#111827;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none\">Verify email</a></p>" +
                       $"<p>Or copy and paste this link into your browser:</p>" +
                       $"<p style=\"word-break:break-all\"><a href=\"{link}\">{link}</a></p>" +
                       $"<p style=\"color:#6b7280;font-size:12px\">This link expires in 24 hours.</p>";
        }

        return new EmailMessage(recipient, subject, textBody, htmlBody);
    }
}
