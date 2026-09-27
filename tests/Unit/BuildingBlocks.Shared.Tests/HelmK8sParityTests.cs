namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.5: Helm must render the same behavior as the k8s manifests.
/// The gaps that bit before: no ingress rewrite (every API path 404),
/// Service port 80 vs 8080, divergent probe timings, envFrom catch-all,
/// missing Retention keys, and a Postgres init env the stock image ignores.
/// </summary>
public class HelmK8sParityTests
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
    public void HelmApiIngress_RewritesServicePrefix()
    {
        var ingress = Read("helm/payment-switch/templates/ingress.yaml");

        // Backends serve /health/live and /api/..., not /<service>/... .
        Assert.Contains("nginx.ingress.kubernetes.io/rewrite-target: /$2", ingress);
        Assert.Contains("pathType: ImplementationSpecific", ingress);
        Assert.Contains("ingressClassName: nginx", ingress);
        foreach (var svc in new[] { "identity", "merchant", "payment", "ledger", "notification", "settlement" })
            Assert.Contains("path: /{{ $svc.name }}(/|$)(.*)", ingress);
    }

    [Fact]
    public void HelmFrontendIngress_HasNoRewrite()
    {
        // merchant-web serves at / and admin-web uses basePath /admin: paths
        // must reach the backends unchanged (mirrors k8s/frontend-ingress.yaml).
        var ingress = Read("helm/payment-switch/templates/frontend-ingress.yaml");

        Assert.DoesNotContain("rewrite-target", ingress);
        Assert.Contains("path: /admin", ingress);
        Assert.Contains("path: /", ingress);
        Assert.Contains("pathType: Prefix", ingress);
    }

    [Fact]
    public void K8sApiIngress_KeepsRewrite()
    {
        Assert.Contains("rewrite-target: /$2", Read("k8s/ingress.yaml"));
    }

    [Fact]
    public void ServicePorts_Are8080ForApis()
    {
        Assert.Contains("port: 8080", Read("helm/payment-switch/templates/service.yaml"));

        foreach (var svc in new[] { "identity", "merchant", "payment", "ledger", "notification", "settlement" })
            Assert.Contains("port: 8080", Read($"k8s/{svc}-deployment.yaml"));
    }

    [Fact]
    public void ProbeTimings_MatchK8s()
    {
        var template = Read("helm/payment-switch/templates/deployment.yaml");
        var payment = Read("k8s/payment-deployment.yaml");

        // Liveness 10s/15s, readiness 15s/20s, startup 5s/10s/30, 5s timeouts.
        foreach (var snippet in new[]
                 {
                     "initialDelaySeconds: 5", "periodSeconds: 10", "failureThreshold: 30",
                     "initialDelaySeconds: 10", "periodSeconds: 15",
                     "initialDelaySeconds: 15", "periodSeconds: 20", "timeoutSeconds: 5",
                 })
        {
            Assert.Contains(snippet, template);
            Assert.Contains(snippet, payment);
        }
    }

    [Fact]
    public void HelmDeployment_UsesExplicitEnv()
    {
        var template = Read("helm/payment-switch/templates/deployment.yaml");

        Assert.DoesNotContain("envFrom", template);
        foreach (var key in new[]
                 {
                     "ConnectionStrings__", "Jwt__Secret", "Jwt__PreviousSecret",
                     "Jwt__Issuer", "Jwt__Audience", "RabbitMQ__HostName",
                     "RabbitMQ__UserName", "RabbitMQ__Password",
                     "Retention__CleanupIntervalMinutes", "Retention__BatchSize",
                     "Retention__MessageRetentionDays", "Retention__BusinessRetentionDays",
                     "WebhookSecretEncryption__Key", "Seed__AdminEmail", "Seed__AdminPassword",
                     "Sms__Enabled",
                 })
            Assert.Contains(key, template);
    }

    [Fact]
    public void RetentionKeys_WiredInBothConfigMaps()
    {
        foreach (var file in new[] { "k8s/configmap.yaml", "helm/payment-switch/templates/configmap.yaml" })
        {
            var config = Read(file);
            Assert.Contains("Retention__CleanupIntervalMinutes", config);
            Assert.Contains("Retention__BatchSize", config);
            Assert.Contains("Retention__MessageRetentionDays", config);
        }
    }

    [Fact]
    public void RetentionBusinessDays_MatchDomainDefaults()
    {
        // Financial records 7 years (2557d); delivered notifications 90d.
        Assert.Contains("Retention__BusinessRetentionDays", Read("k8s/payment-deployment.yaml"));
        Assert.Contains("\"2557\"", Read("k8s/payment-deployment.yaml"));
        Assert.Contains("\"2557\"", Read("k8s/ledger-deployment.yaml"));
        Assert.Contains("\"90\"", Read("k8s/notification-deployment.yaml"));

        var values = Read("helm/payment-switch/values.yaml");
        Assert.Contains("retentionBusinessDays: 2557", values);
        Assert.Contains("retentionBusinessDays: 90", values);
    }

    [Fact]
    public void PostgresInit_UsesScriptOnBothRuntimes()
    {
        // The stock postgres image ignores POSTGRES_MULTIPLE_DATABASES, so
        // both runtimes mount the init script (as compose already does).
        var helmInit = Read("helm/payment-switch/templates/postgres-init-configmap.yaml");
        Assert.Contains("CREATE DATABASE \"PaymentDb\"", helmInit);

        var helmPostgres = Read("helm/payment-switch/templates/postgres.yaml");
        Assert.DoesNotContain("POSTGRES_MULTIPLE_DATABASES", helmPostgres);
        Assert.Contains("/docker-entrypoint-initdb.d", helmPostgres);

        Assert.Contains("/docker-entrypoint-initdb.d", Read("k8s/postgres-deployment.yaml"));
    }
}
