namespace Ledger.Application.Common;

public static class FeeCalculator
{
    /// <summary>
    /// Calculates the processing fee for an amount at the given rate in basis points,
    /// rounded to the nearest minor unit. Returns 0 for zero amounts or a non-positive rate.
    /// </summary>
    public static long Calculate(long amount, int basisPoints)
    {
        if (amount <= 0 || basisPoints <= 0)
            return 0;

        return (amount * basisPoints + 5000) / 10000;
    }
}
