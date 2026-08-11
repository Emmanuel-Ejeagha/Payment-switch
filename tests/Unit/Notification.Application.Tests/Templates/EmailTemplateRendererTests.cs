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
}
