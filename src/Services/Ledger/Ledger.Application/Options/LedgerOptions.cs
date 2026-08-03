namespace Ledger.Application.Options;

public class LedgerOptions
{
    /// <summary>Processing fee charged on capture, in basis points (1 bp = 0.01%).</summary>
    public int FeeBasisPoints { get; set; }
}
