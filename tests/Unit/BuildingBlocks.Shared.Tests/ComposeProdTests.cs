namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.6: compose production story and hygiene. The base file stays
/// HTTP-only; TLS/443 comes exclusively from the prod overlay. Resource
/// bounds apply everywhere, and no VCS metadata may re-enter image contexts.
/// </summary>
public class ComposeProdTests
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

    private static string ServiceBlock(string compose, string service)
    {
        var start = compose.IndexOf($"\n  {service}:", StringComparison.Ordinal);
        Assert.True(start >= 0, $"service '{service}' not found in docker-compose.yml");
        var scan = start + 1;
        while (true)
        {
            var candidate = compose.IndexOf("\n  ", scan, StringComparison.Ordinal);
            if (candidate < 0)
                return compose[start..];
            if (char.IsLetter(compose[candidate + 3]))
                return compose[start..candidate];
            scan = candidate + 1;
        }
    }

    [Fact]
    public void BaseCompose_PublishesNo443()
    {
        Assert.DoesNotContain("\"443:443\"", Read("docker-compose.yml"));
        Assert.DoesNotContain("./infra/nginx/tls:/etc/nginx/tls:ro", Read("docker-compose.yml"));
    }

    [Fact]
    public void ProdOverlay_Publishes443WithTlsMount()
    {
        var overlay = Read("docker-compose.prod.yml");

        Assert.Contains("\"443:443\"", overlay);
        Assert.Contains("./infra/nginx/tls:/etc/nginx/tls:ro", overlay);
        Assert.Contains("docker-compose.prod.yml", overlay);
    }

    [Fact]
    public void EveryService_HasResourceLimits()
    {
        var compose = Read("docker-compose.yml");

        foreach (var service in new[]
                 {
                     "postgres", "redis", "rabbitmq", "jaeger", "prometheus",
                     "alertmanager", "grafana", "identity-api", "merchant-api",
                     "payment-api", "ledger-api", "notification-api",
                     "settlement-api", "merchant-web", "admin-web", "nginx"
                 })
            Assert.Contains("limits:", ServiceBlock(compose, service));
    }

    [Fact]
    public void Dockerignore_ExcludesVcsMetadata()
    {
        var ignore = Read(".dockerignore");

        Assert.Contains("**/.git", ignore);
        Assert.DoesNotContain("!.git/", ignore);
    }

    [Fact]
    public void Runbook_PointsTlsAtProdOverlay()
    {
        Assert.Contains("docker-compose.prod.yml", Read("docs/runbook.md"));
        Assert.Contains("docker-compose.prod.yml", Read("docs/tls.md"));
    }
}
