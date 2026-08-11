using System.Net;
using System.Text;

namespace Notification.Application.Templates;

/// <summary>
/// Renders branded HTML email templates for notification types. The subject
/// selects the template; the (already-substituted) plain-text body is embedded
/// so templates never format money or other payload values themselves.
/// </summary>
public static class EmailTemplateRenderer
{
    private const string Brand = "PaymentSwitch";

    public static string Render(string? subject, string? body)
    {
        var safeBody = WebUtility.HtmlEncode(body ?? string.Empty)
            .Replace("\r\n", "<br>")
            .Replace("\n", "<br>");
        var heading = HeadingFor(subject);
        var accent = AccentColorFor(subject);
        var safeSubject = WebUtility.HtmlEncode(subject ?? heading);

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine($"<title>{safeSubject}</title>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body style=\"margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;\">");
        sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"padding:32px 16px;\">");
        sb.AppendLine("  <tr><td align=\"center\">");
        sb.AppendLine("    <table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:560px;background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 1px 3px rgba(0,0,0,0.08);\">");
        sb.AppendLine($"      <tr><td style=\"background-color:{accent};padding:20px 28px;\">");
        sb.AppendLine($"        <span style=\"color:#ffffff;font-size:18px;font-weight:bold;\">{Brand}</span>");
        sb.AppendLine("      </td></tr>");
        sb.AppendLine("      <tr><td style=\"padding:32px 28px 16px 28px;\">");
        sb.AppendLine($"        <h1 style=\"margin:0 0 16px 0;font-size:22px;color:#111827;\">{WebUtility.HtmlEncode(heading)}</h1>");
        sb.AppendLine($"        <p style=\"margin:0;font-size:15px;line-height:1.6;color:#374151;\">{safeBody}</p>");
        sb.AppendLine("      </td></tr>");
        sb.AppendLine("      <tr><td style=\"padding:24px 28px;border-top:1px solid #e5e7eb;\">");
        sb.AppendLine("        <p style=\"margin:0;font-size:12px;color:#6b7280;\">&copy; PaymentSwitch. This is an automated notification; please do not reply.</p>");
        sb.AppendLine("      </td></tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("  </td></tr>");
        sb.AppendLine("</table>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string HeadingFor(string? subject) => subject switch
    {
        "Payment Authorized" => "Payment authorized",
        "Payment Captured" => "Payment captured",
        "Payment Refunded" => "Refund processed",
        null or "" => "Notification",
        _ => subject
    };

    private static string AccentColorFor(string? subject) => subject switch
    {
        "Payment Refunded" => "#0f766e",
        _ => "#111827"
    };
}
