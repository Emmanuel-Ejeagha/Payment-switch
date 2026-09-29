using BuildingBlocks.Shared.ValueObjects;

namespace BuildingBlocks.Shared.Tests;

public class CurrencyInfoTests
{
    [Theory]
    [InlineData("JPY", 0)]
    [InlineData("KRW", 0)]
    [InlineData("KWD", 3)]
    [InlineData("BHD", 3)]
    [InlineData("USD", 2)]
    [InlineData("EUR", 2)]
    public void Lookup_ReturnsCorrectExponent(string code, int expectedExponent)
    {
        var info = CurrencyInfo.Lookup(code);
        Assert.Equal(expectedExponent, info.MinorUnitsExponent);
        Assert.Equal(code, info.Code);
    }

    [Fact]
    public void ToMajorUnits_JPY_NoScaling()
    {
        var jpy = CurrencyInfo.Lookup("JPY");
        Assert.Equal(1000m, jpy.ToMajorUnits(1000));
    }

    [Fact]
    public void ToMajorUnits_KWD_ThreeDecimals()
    {
        var kwd = CurrencyInfo.Lookup("KWD");
        Assert.Equal(1.234m, kwd.ToMajorUnits(1234));
    }

    [Fact]
    public void ToMinorUnits_KWD_RoundsHalfAway()
    {
        var kwd = CurrencyInfo.Lookup("KWD");
        Assert.Equal(125, kwd.ToMinorUnits(0.125m));
    }

    [Fact]
    public void Lookup_InvalidCode_Throws()
    {
        Assert.Throws<ArgumentException>(() => CurrencyInfo.Lookup("AB"));
        Assert.Throws<ArgumentException>(() => CurrencyInfo.Lookup(""));
        Assert.Throws<ArgumentException>(() => CurrencyInfo.Lookup("TOOLONG"));
    }

    [Fact]
    public void RoundTrip_JPY_KWD_USD()
    {
        var cases = new[] { ("JPY", 12345L), ("KWD", 1234L), ("USD", 12345L) };
        foreach (var (code, minor) in cases)
        {
            var info = CurrencyInfo.Lookup(code);
            var major = info.ToMajorUnits(minor);
            var back = info.ToMinorUnits(major);
            Assert.Equal(minor, back);
        }
    }
}
