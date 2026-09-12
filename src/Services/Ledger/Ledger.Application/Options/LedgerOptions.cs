namespace Ledger.Application.Options;

public class LedgerOptions
{
    /// <summary>100% expressed in basis points: fees can never exceed the captured amount.</summary>
    public const int MaxFeeBasisPoints = 10_000;

    /// <summary>Processing fee charged on capture, in basis points (1 bp = 0.01%).</summary>
    public int FeeBasisPoints { get; set; }
}
