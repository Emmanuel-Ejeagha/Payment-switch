namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.1: one image-naming convention across k8s manifests, the Helm
/// chart, and CI (`<registry>/<namespace>/<name>:<tag>` with CI service
/// names). The raw manifests previously used a third shape
/// (<c>paymentswitch-&lt;svc&gt;:latest</c>) that exists in no registry, and
/// `kubectl set image` only works because deployment/container names happen
/// to match — both are pinned here.
/// </summary>
public class ImageNamingConsistencyTests
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

    // CI service names (ci-cd.yml matrices + set-image targets).
    private static readonly string[] ApiNames =
        ["identity-api", "merchant-api", "payment-api", "ledger-api", "notification-api", "settlement-api"];

    private static readonly string[] WebNames = ["merchant-web", "admin-web"];

    private static string K8sDeployment(string deploymentName)
    {
        // k8s file names use the short service name (identity-deployment.yaml
        // for deployment identity-api; *-web names match as-is).
        var file = deploymentName.EndsWith("-api")
            ? deploymentName[..^"-api".Length]
            : deploymentName;
        return Read($"k8s/{file}-deployment.yaml");
    }

    [Theory]
    [InlineData("identity-api")]
    [InlineData("merchant-api")]
    [InlineData("payment-api")]
    [InlineData("ledger-api")]
    [InlineData("notification-api")]
    [InlineData("settlement-api")]
    [InlineData("merchant-web")]
    [InlineData("admin-web")]
    public void K8sManifest_UsesCanonicalImageName(string name)
    {
        var manifest = K8sDeployment(name);

        // Same default the Helm chart renders (paymentswitch/<name>:latest).
        Assert.Contains($"image: paymentswitch/{name}:latest", manifest);
        // Deployment + container names match the CI `kubectl set image` targets.
        Assert.Contains($"name: {name}", manifest);
    }

    [Theory]
    [InlineData("identity-api")]
    [InlineData("merchant-api")]
    [InlineData("payment-api")]
    [InlineData("ledger-api")]
    [InlineData("notification-api")]
    [InlineData("settlement-api")]
    [InlineData("merchant-web")]
    [InlineData("admin-web")]
    public void Helm_DefaultRepository_MatchesCanonicalName(string name)
    {
        Assert.Contains($"repository: paymentswitch/{name}", Read("helm/payment-switch/values.yaml"));
    }

    [Fact]
    public void CiWorkflow_BuildsAndDeploysCanonicalNames()
    {
        var workflow = Read(".github/workflows/ci-cd.yml");

        // Build pushes matrix-derived <name>:<sha> tags for APIs and webs.
        Assert.Contains("${{ matrix.service.name }}-api:${{ github.sha }}", workflow);
        Assert.Contains("${{ matrix.app.name }}-web:${{ github.sha }}", workflow);
        // Deploy pins deployment + container <name> (must match k8s names above).
        foreach (var name in ApiNames.Concat(WebNames))
            Assert.Contains($"kubectl set image deployment/{name} {name}=", workflow);
    }

    [Fact]
    public void Docs_DeclareSingleConvention()
    {
        Assert.Contains("paymentswitch/<name>:latest", Read("docs/deployment.md"));
        Assert.Contains("kubectl set image", Read("docs/deployment.md"));
    }
}
