using Ledger.Domain.ValueObjects;

namespace Ledger.Domain.Tests;

public class ValueObjectsTests
{
    [Fact]
    public void Money_Valid_ShouldCreate()
    {
        var m = new Money(10L, "usd");
        Assert.Equal(10L, m.Amount);
        Assert.Equal("USD", m.Currency);
    }

    [Fact]
    public void Money_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(-1L, "USD"));
    }

    [Fact]
    public void Money_InvalidCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Money(10L, "US"));
    }

    [Theory]
    [InlineData("USD", 2)]
    [InlineData("JPY", 0)]
    [InlineData("KWD", 3)]
    public void Money_MinorUnitsExponent_UsesCurrencyInfo(string currency, int expectedExponent)
    {
        var m = new Money(100L, currency);

        Assert.Equal(expectedExponent, m.MinorUnitsExponent);
    }

    [Theory]
    [InlineData("USD", 12345L, 123.45)]
    [InlineData("JPY", 12345L, 12345.0)]
    [InlineData("KWD", 12345L, 12.345)]
    public void Money_ToMajorAmount_UsesCurrencyExponent(string currency, long amount, decimal expectedMajor)
    {
        var m = new Money(amount, currency);

        Assert.Equal(expectedMajor, m.ToMajorAmount());
    }

    [Theory]
    [InlineData("USD", 123.45, 12345L)]
    [InlineData("JPY", 123.4, 123L)]
    [InlineData("KWD", 12.345, 12345L)]
    public void Money_FromMajorAmount_UsesCurrencyExponent(string currency, decimal majorAmount, long expectedMinor)
    {
        var m = Money.FromMajorAmount(majorAmount, currency);

        Assert.Equal(expectedMinor, m.Amount);
        Assert.Equal(currency.ToUpperInvariant(), m.Currency);
    }

    [Fact]
    public void Money_FromMajorAmount_RoundTrips()
    {
        var original = new Money(12345L, "USD");

        Assert.Equal(original, Money.FromMajorAmount(original.ToMajorAmount(), original.Currency));
    }

    [Fact]
    public void Money_FromMajorAmount_Negative_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.FromMajorAmount(-1m, "USD"));
    }

    [Fact]
    public void Currency_Valid_ShouldCreate()
    {
        var c = new Currency("usd");
        Assert.Equal("USD", c.Code);
    }

    [Fact]
    public void Currency_Invalid_Throws()
    {
        Assert.Throws<ArgumentException>(() => new Currency("US"));
    }

    [Fact]
    public void CorrelationId_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CorrelationId(""));
    }

    [Fact]
    public void CorrelationId_Valid_ShouldSet()
    {
        var cid = new CorrelationId("test-id");
        Assert.Equal("test-id", cid.Value);
    }
}