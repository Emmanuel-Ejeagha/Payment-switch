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

        if (basisPoints > Options.LedgerOptions.MaxFeeBasisPoints)
            throw new ArgumentOutOfRangeException(nameof(basisPoints),
                $"Fee rate cannot exceed {Options.LedgerOptions.MaxFeeBasisPoints} basis points (100%).");

        // Unchecked long multiplication would silently wrap for huge amounts;
        // reject instead of posting a wrong (possibly negative) fee.
        if (amount > (long.MaxValue - 5000) / basisPoints)
            throw new InvalidOperationException($"Amount {amount} is too large to compute fees for.");

        return (amount * basisPoints + 5000) / 10000;
    }
}
