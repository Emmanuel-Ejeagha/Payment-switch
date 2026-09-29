namespace BuildingBlocks.Shared.Retention;

/// <summary>
/// Immutable JSON-snapshot row moved out of a live table by the retention sweep.
/// One uniform table per service keeps archival schema-agnostic: a row can always
/// be restored to the original table from <see cref="Payload"/>, even if the live
/// schema evolves later. Stores the full record, so the archive is the audit trail
/// for records that must survive regulatory retention timelines (see RETENTION.md).
/// </summary>
public sealed class ArchivedRecord
{
    public Guid Id { get; private set; }

    /// <summary>Logical name of the source table, e.g. "PaymentIntents".</summary>
    public string SourceTable { get; private set; }

    /// <summary>JSON snapshot of the archived row (including owned child records).</summary>
    public string Payload { get; private set; }

    public DateTime ArchivedAt { get; private set; }

    private ArchivedRecord()
    {
    }

    public ArchivedRecord(Guid id, string sourceTable, string payload, DateTime archivedAt)
    {
        Id = id;
        SourceTable = sourceTable;
        Payload = payload;
        ArchivedAt = archivedAt;
    }
}