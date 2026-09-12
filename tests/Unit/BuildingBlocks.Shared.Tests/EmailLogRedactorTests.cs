using BuildingBlocks.Shared.Email;

namespace BuildingBlocks.Shared.Tests;

public class EmailLogRedactorTests
{
    [Fact]
    public void Redact_DoesNotContainInput()
    {
        var body = "Click https://app/verify?token=super-secret-token-123 to confirm.";

        var redacted = EmailLogRedactor.Redact(body);

        Assert.DoesNotContain("super-secret-token-123", redacted);
        Assert.DoesNotContain(body, redacted);
    }

    [Fact]
    public void Redact_IsDeterministic()
    {
        Assert.Equal(EmailLogRedactor.Redact("same body"), EmailLogRedactor.Redact("same body"));
    }

    [Fact]
    public void Redact_DistinguishesBodies()
    {
        Assert.NotEqual(EmailLogRedactor.Redact("body-a"), EmailLogRedactor.Redact("body-b"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Redact_EmptyInput_ReturnsPlaceholder(string? body)
    {
        Assert.Equal("(empty)", EmailLogRedactor.Redact(body));
    }
}
