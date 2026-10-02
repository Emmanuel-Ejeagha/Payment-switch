using Notification.Application.Services;

namespace Notification.Application.Tests.Services;

public class VerificationEmailBuilderTests
{
    [Fact]
    public void Build_CreatesLinkFromTrustedOriginOnly()
    {
        var email = VerificationEmailBuilder.Build(
            "user@example.com", "TOKEN123",
            new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            "Confirm your email address", "https://merchant.example.com/");

        Assert.Equal("https://merchant.example.com/verify-email?email=user%40example.com&token=TOKEN123", email.Link);
        Assert.Equal("24 hours", email.ExpiryLabel);
        Assert.Contains(email.Link, email.TextBody);
        Assert.Contains("did not register", email.TextBody);
    }

    [Fact]
    public void Build_WithoutBaseUrl_OmitsLink()
    {
        var email = VerificationEmailBuilder.Build(
            "user@example.com", "TOKEN123",
            new DateTime(2026, 9, 30, 10, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            "Confirm your email address", "");

        Assert.Equal(string.Empty, email.Link);
        Assert.Contains("TOKEN123", email.TextBody);
    }

    [Fact]
    public void Build_RejectsBlankInputs()
    {
        Assert.Throws<ArgumentException>(() => VerificationEmailBuilder.Build(
            "", "TOKEN", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Subject", "https://x.example"));
        Assert.Throws<ArgumentException>(() => VerificationEmailBuilder.Build(
            "a@b.c", "", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), "Subject", "https://x.example"));
    }
}
