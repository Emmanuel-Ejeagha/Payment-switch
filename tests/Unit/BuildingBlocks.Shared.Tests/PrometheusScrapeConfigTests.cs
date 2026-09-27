namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 8.1: pin the Prometheus scrape contract across runtimes. The APIs
/// expose OpenMetrics at /metrics (verified live: HTTP 200), the scrape
/// configs must name that path explicitly per job, RabbitMQ needs its
/// plugin enabled wherever the 15692 target is declared, and the documented
/// smoke URLs must be real nginx-routed paths (no /api/v1/health).
/// </summary>
public class PrometheusScrapeConfigTests
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

    private static readonly string[] ApiTargets =
        ["identity-api:8080", "merchant-api:8080", "payment-api:8080",
         "ledger-api:8080", "notification-api:8080", "settlement-api:8080"];

    [Fact]
    public void ComposePrometheus_DeclaresExplicitMetricsPathPerJob()
    {
        var yml = Read("infra/prometheus/prometheus.yml");

        foreach (var target in ApiTargets)
            Assert.Contains($"targets: [\"{target}\"]", yml);
        Assert.Contains("targets: [\"rabbitmq:15692\"]", yml);
        // One explicit path per API job + rabbitmq (7 total).
        Assert.Equal(7, CountOccurrences(yml, "metrics_path: /metrics"));
    }

    [Fact]
    public void K8sPrometheus_DeclaresExplicitMetricsPathPerJob()
    {
        var configmap = Read("k8s/prometheus-configmap.yaml");

        foreach (var target in ApiTargets)
            Assert.Contains($"targets: ['{target}']", configmap);
        Assert.Contains("targets: ['rabbitmq:15692']", configmap);
        Assert.Equal(7, CountOccurrences(configmap, "metrics_path: '/metrics'"));
    }

    [Fact]
    public void HelmPrometheus_DeclaresMetricsPathAndPortRewrite()
    {
        var template = Read("helm/payment-switch/templates/prometheus.yaml");

        Assert.Contains("metrics_path: '/metrics'", template);
        Assert.Contains("replacement: $1:8080", template);
        Assert.Contains("targets: ['rabbitmq:15692']", template);
    }

    [Fact]
    public void RabbitMq_PrometheusPluginEnabledEverywhere()
    {
        Assert.Contains("rabbitmq_prometheus", Read("docker-compose.yml"));
        Assert.Contains("rabbitmq_prometheus", Read("k8s/rabbitmq-deployment.yaml"));
        Assert.Contains("rabbitmq_prometheus", Read("helm/payment-switch/templates/rabbitmq.yaml"));
    }

    [Fact]
    public void Docs_ContainNoStaleHealthSmokeUrls()
    {
        foreach (var doc in new[] { "docs/runbook.md", "docs/tls.md", "docs/secrets.md" })
            Assert.DoesNotContain("/api/v1/health", Read(doc));
    }

    [Fact]
    public void Nginx_StillDeniesMetricsPublicly()
    {
        // Defense-in-depth (TASK-046) must survive: the public ingress 404s
        // /metrics while Prometheus scrapes the container network directly.
        foreach (var conf in new[] { "infra/nginx/http/server.conf", "infra/nginx/tls/server.conf" })
            Assert.Contains("metrics", Read(conf));
    }

    [Fact]
    public void ComposeSmokeScript_CoversEveryApi()
    {
        var script = Read("infra/smoke/compose-smoke.sh");

        foreach (var prefix in new[] { "identity", "merchant", "payment", "ledger", "notification", "settlement" })
            Assert.Contains(prefix, script);
        Assert.Contains("/health/live", script);
        Assert.Contains("/metrics", script);
        Assert.Contains("15692", script);
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
}
