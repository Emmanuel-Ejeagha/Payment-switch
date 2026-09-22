namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 10.1: backups must exist as a job (not just documentation) and the
/// restore path must be executable. Pins the backup service/volume wiring,
/// the drill's coverage of all six databases, and the runbook procedure.
/// </summary>
public class BackupRestoreTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "PaymentSwitch.slnx")))
            dir = dir.Parent;
        if (dir is null)
            throw new InvalidOperationException("Could not locate repo root (PaymentSwitch.slnx).");
        return dir.FullName;
    }

    private static string Read(string relative) =>
        File.ReadAllText(Path.Combine(RepoRoot(), relative));

    private static readonly string[] Databases =
        ["IdentityDb", "MerchantDb", "PaymentDb", "LedgerDb", "NotificationDb", "SettlementDb"];

    [Fact]
    public void Compose_RunsNightlyBackupJob()
    {
        var compose = Read("docker-compose.yml");

        Assert.Contains("backup:", compose);
        Assert.Contains("pg-backup.sh", compose);
        Assert.Contains("pgbackups:/backups", compose);
        Assert.Contains("pgbackups:", compose);
    }

    [Fact]
    public void BackupScript_CoversAllDatabasesWithManifest()
    {
        var script = Read("infra/backup/pg-backup.sh");

        foreach (var db in Databases)
            Assert.Contains(db, script);
        Assert.Contains("SHA256SUMS", script);
        Assert.Contains("pg_dumpall", script);
        Assert.Contains("KEEP_DAILY", script);
    }

    [Fact]
    public void RestoreDrill_VerifiesEveryDatabaseAgainstScratch()
    {
        var drill = Read("infra/backup/restore-drill.sh");

        // Never touches the live volume: scratch container only.
        Assert.Contains("paymentswitch-restore-drill", drill);
        Assert.DoesNotContain("postgres_data", drill);
        foreach (var db in Databases)
            Assert.Contains(db, drill);
        // Core-table sanity per database (check_table quotes identifiers).
        foreach (var check in new[]
                 {
                     "check_table IdentityDb Users", "check_table MerchantDb Merchants",
                     "check_table PaymentDb PaymentIntents", "check_table LedgerDb JournalEntries",
                     "check_table NotificationDb Notifications", "check_table SettlementDb SettlementBatches"
                 })
            Assert.Contains(check, drill);
        Assert.Contains("DRILL PASSED", drill);
    }

    [Fact]
    public void Runbook_DocumentsJobAndDrill()
    {
        var runbook = Read("docs/runbook.md");

        Assert.Contains("pg-backup.sh", runbook);
        Assert.Contains("restore-drill.sh", runbook);
        Assert.Contains("ops log", runbook);
    }
}
