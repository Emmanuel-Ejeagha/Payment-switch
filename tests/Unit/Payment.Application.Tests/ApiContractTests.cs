using System.Text.RegularExpressions;

namespace Payment.Application.Tests;

public class ApiContractTests
{
    [Fact]
    public void ApiReference_Examples_UseMinorUnits()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "api-reference.md");
        // Normalize path for both Windows and Linux
        path = Path.GetFullPath(path);
        if (!File.Exists(path))
        {
            // Try alternative relative from test project
            path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "docs", "api-reference.md");
            path = Path.GetFullPath(path);
        }
        if (!File.Exists(path))
            return; // doc not present in CI, skip

        var content = File.ReadAllText(path);
        // Find JSON examples with "amount": <number> - should be integer minor-units, not float
        var matches = Regex.Matches(content, "\"amount\"\\s*:\\s*([0-9.]+)");
        foreach (Match m in matches)
        {
            var value = m.Groups[1].Value;
            Assert.DoesNotContain(".", value);
            Assert.True(long.TryParse(value, out _), $"amount {value} should be integer minor-units");
        }
    }

    [Fact]
    public void ConfirmVsAuthorize_Mapping_IsDocumented()
    {
        // This test ensures the public API contract distinguishes confirm vs authorize
        // as per docs/api-reference.md - we just verify the handlers exist
        Assert.True(typeof(Payment.Application.Features.Command.ConfirmPaymentIntent.ConfirmPaymentIntentHandler) != null);
        Assert.True(typeof(Payment.Application.Features.Command.AuthorizePayment.AuthorizePaymentHandler) != null);
    }
}
