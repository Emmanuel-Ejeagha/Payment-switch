using System.Text.RegularExpressions;

namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.2: NetworkPolicy selectors must match real pod labels. The file
/// previously selected app.kubernetes.io/part-of (carried by zero pods), so
/// under enforcement every allow-rule applied to nothing. These tests pin
/// both directions: no dangling selectors, and every workload is selected
/// by at least one policy.
/// </summary>
public partial class NetworkPolicySelectorTests
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

    private static HashSet<string> DeployedAppLabels()
    {
        var labels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.GetFiles(Path.Combine(RepoRoot(), "k8s"), "*-deployment.yaml"))
        {
            foreach (Match m in AppLabelRegex().Matches(File.ReadAllText(file)))
                labels.Add(m.Groups[1].Value);
        }
        return labels;
    }

    private static HashSet<string> PolicySelectedApps(string policies)
    {
        var selected = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in AppLabelRegex().Matches(policies))
            selected.Add(m.Groups[1].Value);
        foreach (Match m in InListRegex().Matches(policies))
        {
            foreach (var value in m.Groups[1].Value.Split(','))
            {
                var trimmed = value.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    selected.Add(trimmed);
            }
        }
        return selected;
    }

    [Fact]
    public void Policies_ContainNoPartOfSelectors()
    {
        Assert.DoesNotContain("app.kubernetes.io/part-of", Read("k8s/networkpolicy.yaml"));
    }

    [Fact]
    public void AllDeployedApps_AreSelectedBySomePolicy()
    {
        var deployed = DeployedAppLabels();
        var selected = PolicySelectedApps(Read("k8s/networkpolicy.yaml"));

        Assert.Subset(selected, deployed);
    }

    [Fact]
    public void PolicySelectors_ReferenceOnlyDeployedApps()
    {
        // A typo'd label selects nothing (the original part-of bug class).
        // External peers use distinct keys (app.kubernetes.io/name, k8s-app)
        // and are never collected as `app:` values.
        var deployed = DeployedAppLabels();
        var selected = PolicySelectedApps(Read("k8s/networkpolicy.yaml"));

        Assert.Subset(deployed, selected);
    }

    [Fact]
    public void EveryWorkload_IsSelectedByAtLeastOnePolicy()
    {
        var policies = Read("k8s/networkpolicy.yaml");
        var selected = PolicySelectedApps(policies);

        foreach (var app in new[]
                 {
                     "identity-api", "merchant-api", "payment-api", "ledger-api",
                     "notification-api", "settlement-api", "merchant-web", "admin-web",
                     "postgres", "rabbitmq", "redis", "jaeger", "prometheus",
                     "grafana", "alertmanager"
                 })
            Assert.Contains(app, selected);
    }

    [Fact]
    public void IpBlockPeers_CombineWithNoPodOrNamespaceSelector()
    {
        // An ipBlock peer mixed with pod/namespace selectors is rejected by
        // API validation (previously the SMTP rule); each ipBlock must stand
        // alone in its peer entry.
        var lines = Read("k8s/networkpolicy.yaml").Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("ipBlock:"))
                continue;
            for (var j = Math.Max(0, i - 3); j < i; j++)
            {
                Assert.DoesNotContain("namespaceSelector", lines[j]);
                Assert.DoesNotContain("podSelector", lines[j]);
            }
        }
        Assert.Contains("ipBlock:", string.Join('\n', lines));
    }

    [GeneratedRegex(@"^\s*app:\s*([a-z0-9-]+)\s*$", RegexOptions.Multiline)]
    private static partial Regex AppLabelRegex();

    [GeneratedRegex(@"values:\s*\[(.*?)\]")]
    private static partial Regex InListRegex();
}
