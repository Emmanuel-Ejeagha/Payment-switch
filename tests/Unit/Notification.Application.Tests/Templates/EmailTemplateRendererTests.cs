using Notification.Application.Services;
using Notification.Application.Templates;

namespace Notification.Application.Tests.Templates;

public class EmailTemplateRendererTests
{
    [Fact]
    public void Render_PaymentAuthorized_ContainsHeadingAndPayload()
    {
        var html = EmailTemplateRenderer.Render("Payment Authorized", "A payment of 1999 USD was authorized for your account.");

        Assert.Contains("Payment authorized", html);
        Assert.Contains("1999", html);
        Assert.Contains("USD", html);
        Assert.Contains("PaymentSwitch", html);
    }

    [Fact]
    public void Render_PaymentCaptured_ContainsHeadingAndPayload()
    {
        var html = EmailTemplateRenderer.Render("Payment Captured", "A payment of 5000 EUR was captured for your account.");

        Assert.Contains("Payment captured", html);
        Assert.Contains("5000", html);
        Assert.Contains("EUR", html);
    }

    [Fact]
    public void Render_PaymentRefunded_ContainsHeadingAndPayload()
    {
        var html = EmailTemplateRenderer.Render("Payment Refunded", "A refund of 750 GBP was processed for your account.");

        Assert.Contains("Refund processed", html);
        Assert.Contains("750", html);
        Assert.Contains("GBP", html);
    }

    [Fact]
    public void Render_UnknownSubject_FallsBackToSubject()
    {
        var html = EmailTemplateRenderer.Render("Settlement scheduled", "Your settlement is scheduled.");

        Assert.Contains("Settlement scheduled", html);
    }

    [Fact]
    public void Render_EscapesHtmlInBody()
    {
        var html = EmailTemplateRenderer.Render("Payment Authorized", "<script>alert(1)</script> & co");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&amp; co", html);
    }

    [Fact]
    public void Render_ProducesHtmlDocumentWithBrandedFooter()
    {
        var html = EmailTemplateRenderer.Render("Payment Authorized", "Body");

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("This is an automated notification", html);
        Assert.Contains("automated notification", html);
    }

    [Fact]
    public void RenderVerification_ContainsActionExpiryAndFallback()
    {
        var email = VerificationEmailBuilder.Build(
            "user@example.com", "ABCDEF1234",
            new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            "Confirm your email address", "https://merchant.example.com");

        var html = EmailTemplateRenderer.RenderVerification(email);

        Assert.Contains("Confirm your email address", html);
        Assert.Contains("Verify email", html);
        Assert.Contains("https://merchant.example.com/verify-email?email=user%40example.com&amp;token=ABCDEF1234", html);
        Assert.Contains("24 hours", html);
        Assert.Contains("did not register", html);
        Assert.Contains("PaymentSwitch", html);
    }

    [Fact]
    public void RenderVerification_EscapesLinkAndExpiry()
    {
        var email = new VerificationEmail(
            "user@example.com", "Confirm <b>it</b>", "body",
            "https://example.com/verify?a=\"x\"", "24 <hours>");

        var html = EmailTemplateRenderer.RenderVerification(email);

        Assert.DoesNotContain("<b>it</b>", html);
        Assert.DoesNotContain("a=\"x\"", html);
        Assert.Contains("&lt;hours&gt;", html);
    }
}
