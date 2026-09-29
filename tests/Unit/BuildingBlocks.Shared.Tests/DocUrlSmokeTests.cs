using System.Text.RegularExpressions;

namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.7: documentation must stay executable. These tests pin the four
/// corrected areas: complete secret keys, integer minor-unit amounts,
/// real nginx-routed health paths, and compose-reachable load-test URLs.
/// </summary>
public partial class DocUrlSmokeTests
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
    public void NoDoc_ReferencesStaleHealthPaths()
    {
        // Health lives at /health/live|ready on each API; nginx strips the
        // /<service>/ prefix. The /api/v1/health form never existed.
        foreach (var doc in Directory.GetFiles(Path.Combine(RepoRoot(), "docs"), "*.md"))
            Assert.DoesNotContain("/api/v1/health", File.ReadAllText(doc));
    }

    [Fact]
    public void NoDocOrScript_PointsAtUnboundLocalhost8080()
    {
        // Nothing listens on host :8080 in compose (APIs are on loopback
        // service ports behind nginx :80). Reachable bases only.
        foreach (var doc in Directory.GetFiles(Path.Combine(RepoRoot(), "docs"), "*.md"))
            Assert.DoesNotContain("localhost:8080", File.ReadAllText(doc));
        foreach (var script in Directory.GetFiles(Path.Combine(RepoRoot(), "tests/load"), "*.js", SearchOption.AllDirectories))
            Assert.DoesNotContain("localhost:8080", File.ReadAllText(script));
    }

    [Fact]
    public void ApiReference_AmountExampleIsIntegerMinorUnits()
    {
        var reference = Read("docs/api-reference.md");

        Assert.DoesNotContain("100.00", reference);
        Assert.Contains("\"amount\": 10000", reference);
        Assert.Contains("minor units", reference);
    }

    [Fact]
    public void DeploymentSecretCommand_CoversAllRequiredKeys()
    {
        // Every secretKeyRef key must be creatable from the documented
        // command (minus the explicitly optional rotation key).
        var example = Read("k8s/secret.example.yaml");
        var stringData = example.Substring(example.IndexOf("stringData:", StringComparison.Ordinal));
        var exampleKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in SecretKeyRegex().Matches(stringData))
            exampleKeys.Add(m.Groups[1].Value);
        Assert.NotEmpty(exampleKeys);

        var deployment = Read("docs/deployment.md");
        foreach (var key in exampleKeys)
        {
            if (key == "Jwt__PreviousSecret")
            {
                Assert.Contains("Jwt__PreviousSecret", deployment);
                continue;
            }
            Assert.Contains($"--from-literal={key}", deployment);
        }
    }

    [Fact]
    public void TlsAndRunbook_UseNginxRoutedHealthPaths()
    {
        Assert.Contains("/identity/health/live", Read("docs/runbook.md"));
        Assert.Contains("/merchant/health/live", Read("docs/tls.md"));
        Assert.Contains("/identity/health/live", Read("docs/secrets.md"));
    }

    [Fact]
    public void LoadTests_DefaultToNginxRoutes()
    {
        Assert.Contains("http://localhost/payment", Read("tests/load/lib/payment-flow.js"));
        Assert.Contains("http://localhost/identity", Read("tests/load/lib/auth.js"));
    }

    [GeneratedRegex(@"^  ([A-Za-z_][A-Za-z0-9_]*):", RegexOptions.Multiline)]
    private static partial Regex SecretKeyRegex();
}
