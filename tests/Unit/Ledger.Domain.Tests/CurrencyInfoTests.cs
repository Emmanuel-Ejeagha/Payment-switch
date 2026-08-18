using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class CurrencyInfoTests
{
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    [InlineData("GBP", 2)]
    [InlineData("jpy", 0)]
    [InlineData("KRW", 0)]
    [InlineData("KWD", 3)]
    [InlineData("BHD", 3)]
    [InlineData("OMR", 3)]
    [InlineData("XTS", 2)]
    public void Lookup_ReturnsExpectedExponent(string code, int expectedExponent)
    {
        var info = CurrencyInfo.Lookup(code);

        Assert.Equal(expectedExponent, info.MinorUnitsExponent);
        Assert.Equal(code.ToUpperInvariant(), info.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("USDD")]
    public void Lookup_InvalidCode_Throws(string? code)
    {
        Assert.Throws<ArgumentException>(() => CurrencyInfo.Lookup(code!));
    }

    [Theory]
    [InlineData("USD", 12345L, 123.45)]
    [InlineData("USD", 100L, 1.00)]
    [InlineData("JPY", 12345L, 12345.0)]
    [InlineData("KWD", 12345L, 12.345)]
    public void ToMajorUnits_UsesCurrencyExponent(string code, long minorUnits, decimal expectedMajor)
    {
        var info = CurrencyInfo.Lookup(code);

        Assert.Equal(expectedMajor, info.ToMajorUnits(minorUnits));
    }

    [Theory]
    [InlineData("USD", 123.45, 12345L)]
    [InlineData("JPY", 123.4, 123L)]
    [InlineData("KWD", 12.345, 12345L)]
    public void ToMinorUnits_UsesCurrencyExponent(string code, decimal majorUnits, long expectedMinor)
    {
        var info = CurrencyInfo.Lookup(code);

        Assert.Equal(expectedMinor, info.ToMinorUnits(majorUnits));
    }

    [Fact]
    public void ToMinorUnits_RoundsHalfAwayFromZero()
    {
        var info = CurrencyInfo.Lookup("KWD");

        Assert.Equal(125L, info.ToMinorUnits(0.125m));
        Assert.Equal(124L, info.ToMinorUnits(0.124m));
    }

    [Fact]
    public void Lookup_ReturnsValueObjectsWithSameCodeAndExponent()
    {
        var a = CurrencyInfo.Lookup("usd");
        var b = CurrencyInfo.Lookup("USD");

        Assert.Equal(a, b);
    }
}
