using Payment.Domain.Services;

namespace Payment.Domain.Tests;

public class CardValidationTests
{
    [Theory]
    [InlineData("4242424242424242", true)]
    [InlineData("4000056655665556", true)]
    [InlineData("4242424242424241", false)]
    [InlineData("1234", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidLuhn_ShouldValidateChecksum(string? number, bool expected)
    {
        Assert.Equal(expected, CardValidation.IsValidLuhn(number!));
    }

    [Theory]
    [InlineData("4242424242424242", "Visa")]
    [InlineData("4111111111111111", "Visa")]
    [InlineData("5555555555554444", "Mastercard")]
    [InlineData("5105105105105100", "Mastercard")]
    [InlineData("378282246310005", "Amex")]
    [InlineData("6011111111111117", "Discover")]
    [InlineData("123456", "Unknown")]
    public void DetectBrand_ShouldIdentifyCardBrand(string number, string expected)
    {
        Assert.Equal(expected, CardValidation.DetectBrand(number));
    }

    [Theory]
    [InlineData(1, 2030, true)]
    [InlineData(12, 2026, true)]
    [InlineData(13, 2030, false)]
    [InlineData(0, 2030, false)]
    [InlineData(5, 2020, false)]
    public void IsValidExpiry_ShouldCheckExpiryDate(int month, int year, bool expected)
    {
        Assert.Equal(expected, CardValidation.IsValidExpiry(month, year));
    }
}
