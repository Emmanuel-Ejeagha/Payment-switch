using Ledger.Application.Common;

namespace Ledger.Application.Tests.Common;

public class FeeCalculatorTests
{
    [Theory]
    [InlineData(10000L, 150, 150L)]
    [InlineData(100L, 150, 2L)]
    [InlineData(200L, 150, 3L)]
    [InlineData(1000L, 0, 0L)]
    [InlineData(0L, 150, 0L)]
    public void Calculate_AppliesBasisPointsRoundedToNearestMinorUnit(long amount, int basisPoints, long expected)
    {
        Assert.Equal(expected, FeeCalculator.Calculate(amount, basisPoints));
    }

    [Fact]
    public void Calculate_HugeAmount_ThrowsInsteadOfOverflowing()
    {
        Assert.Throws<InvalidOperationException>(() => FeeCalculator.Calculate(long.MaxValue, 150));
    }

    [Theory]
    [InlineData(10001)]
    [InlineData(int.MaxValue)]
    public void Calculate_RateAboveHundredPercent_Throws(int basisPoints)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FeeCalculator.Calculate(10000L, basisPoints));
    }

    [Fact]
    public void Calculate_MaxRate_EqualsAmount()
    {
        Assert.Equal(10000L, FeeCalculator.Calculate(10000L, 10_000));
    }

    [Fact]
    public void Calculate_FeeNeverExceedsAmount()
    {
        Assert.True(FeeCalculator.Calculate(9999L, 10_000) <= 9999L);
    }
}
