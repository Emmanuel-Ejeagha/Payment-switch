using System.Text.RegularExpressions;

namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.4: singleton workloads must survive drains/restarts. Pins PDBs that
/// permit eviction, Recreate for the RWO database, broker probes, the
/// Alertmanager backing volume, frontend HEALTHCHECKs, and deterministic
/// k8s web probes.
/// </summary>
public partial class DeploymentResilienceTests
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

    [Fact]
    public void Pdbs_AllowSingletonEviction()
    {
        var pdb = Read("k8s/pdb.yaml");

        // minAvailable: 1 on replicas: 1 forbids every voluntary disruption
        // (drains hang forever). maxUnavailable: 1 lets the singleton move.
        Assert.DoesNotContain("minAvailable", pdb);
        Assert.Equal(8, CountOccurrences(pdb, "maxUnavailable: 1"));
    }

    [Fact]
    public void PdbSelectors_MatchDeployedApps()
    {
        var pdb = Read("k8s/pdb.yaml");
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in AppLabelRegex().Matches(pdb))
            selected.Add(m.Groups[1].Value);

        var deployed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "k8s"), "*-deployment.yaml"))
        {
            foreach (Match m in AppLabelRegex().Matches(File.ReadAllText(file)))
                deployed.Add(m.Groups[1].Value);
        }

        // No dangling selectors (a typo selects nothing, silently).
        Assert.Subset(deployed, selected);
        // The eight scaled workloads are protected; stateful singletons
        // (postgres/rabbitmq/...) intentionally have no PDB — eviction just
        // reschedules them, and a min-available budget would block drains.
        foreach (var app in new[] { "identity-api", "merchant-api", "payment-api", "ledger-api", "notification-api", "settlement-api", "merchant-web", "admin-web" })
            Assert.Contains(app, selected);
    }

    [Fact]
    public void Postgres_UsesRecreateStrategy()
    {
        var manifest = Read("k8s/postgres-deployment.yaml");

        // A second Postgres pod must never run against the same ReadWriteOnce
        // volume (split-brain / mount fight on RollingUpdate).
        Assert.Contains("type: Recreate", manifest);
        Assert.DoesNotContain("RollingUpdate", manifest);
    }

    [Fact]
    public void RabbitMq_HasStartupLivenessReadinessProbes()
    {
        var manifest = Read("k8s/rabbitmq-deployment.yaml");

        Assert.Contains("startupProbe:", manifest);
        Assert.Contains("livenessProbe:", manifest);
        Assert.Contains("readinessProbe:", manifest);
        Assert.Contains("rabbitmq-diagnostics", manifest);
    }

    [Fact]
    public void Alertmanager_StoragePathHasBackingVolume()
    {
        var manifest = Read("k8s/alertmanager-deployment.yaml");

        Assert.Contains("mountPath: /alertmanager", manifest);
        Assert.Contains("emptyDir:", manifest);
    }

    [Theory]
    [InlineData("merchant", "3000", "/api/health")]
    [InlineData("admin", "3001", "/admin/api/health")]
    public void FrontendDockerfile_HasHealthcheckOnAppPort(string app, string port, string path)
    {
        var dockerfile = Read($"apps/{app}/Dockerfile");

        Assert.Contains("HEALTHCHECK", dockerfile);
        // IPv4 loopback: Alpine resolves localhost to ::1 while Next listens on
        // IPv4 only; admin needs its basePath prefix to hit the route.
        Assert.Contains($"http://127.0.0.1:{port}{path}", dockerfile);
    }

    [Theory]
    [InlineData("merchant-web")]
    [InlineData("admin-web")]
    public void K8sWebProbes_HitDeterministicHealthRoute(string deployment)
    {
        var manifest = Read($"k8s/{deployment}-deployment.yaml");

        Assert.Contains("startupProbe:", manifest);
        Assert.Contains("livenessProbe:", manifest);
        Assert.Contains("readinessProbe:", manifest);
        // The generic page root is not a health signal; /api/health is.
        Assert.DoesNotContain("path: /admin", manifest);
        Assert.Equal(3, CountOccurrences(manifest, "path: /api/health"));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }

    [GeneratedRegex(@"^\s*app:\s*([a-z0-9-]+)\s*$", RegexOptions.Multiline)]
    private static partial Regex AppLabelRegex();
}
