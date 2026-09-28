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
}
