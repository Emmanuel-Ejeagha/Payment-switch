namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.8: the CI gates themselves are tested. If any of these fail, the
/// corresponding pipeline protection regressed — fix the workflow, not the
/// test. (Runs without Docker: pure config assertions.)
/// </summary>
public class CiPipelineTests
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
    public void CiCd_ValidatesManifests()
    {
        var workflow = Read(".github/workflows/ci-cd.yml");

        Assert.Contains("helm lint helm/payment-switch", workflow);
        Assert.Contains("helm template payment-switch helm/payment-switch", workflow);
        Assert.Contains("kubeconform -strict -summary -skip ClusterIssuer k8s/", workflow);
        Assert.Contains("kubeconform -strict -summary /tmp/helm-rendered.yaml", workflow);
    }

    [Fact]
    public void CiCd_DeployRequiresManifestValidation()
    {
        Assert.Contains("manifest-validate", Read(".github/workflows/ci-cd.yml"));
    }

    [Fact]
    public void CiCd_AuditsNuGetPackages()
    {
        var workflow = Read(".github/workflows/ci-cd.yml");

        Assert.Contains("dotnet list PaymentSwitch.slnx package --vulnerable --include-transitive", workflow);
    }

    [Fact]
    public void NuGetAuditFailures_BreakTheBuild()
    {
        // The workflow step reports; Directory.Build.props is the teeth.
        var props = Read("Directory.Build.props");

        foreach (var warning in new[] { "NU1901", "NU1902", "NU1903", "NU1904" })
            Assert.Contains(warning, props);
        Assert.Contains("WarningsAsErrors", props);
    }

    [Fact]
    public void CiCd_PrePullsContainerDependencies()
    {
        var workflow = Read(".github/workflows/ci-cd.yml");

        Assert.Contains("docker pull postgres:16-alpine", workflow);
        Assert.Contains("docker pull rabbitmq:3-management-alpine", workflow);
    }

    [Fact]
    public void E2ENightly_RunsCrossServiceSuiteOnSchedule()
    {
        var workflow = Read(".github/workflows/e2e-nightly.yml");

        Assert.Contains("cron:", workflow);
        Assert.Contains("workflow_dispatch", workflow);
        Assert.Contains("E2E.IntegrationTests", workflow);
        Assert.Contains("docker pull rabbitmq:3-management-alpine", workflow);
    }

    [Fact]
    public void K6Nightly_PointsAtExistingScript()
    {
        var workflow = Read(".github/workflows/k6-nightly.yml");

        Assert.Contains("tests/Performance/k6-smoke.js", workflow);
        Assert.True(File.Exists(Path.Combine(RepoRoot(), "tests/Performance/k6-smoke.js")));
    }

    [Fact]
    public void BranchProtection_RequiresManifestValidation()
    {
        Assert.Contains("Manifest Validation", Read("docs/branch-protection.md"));
    }
}
