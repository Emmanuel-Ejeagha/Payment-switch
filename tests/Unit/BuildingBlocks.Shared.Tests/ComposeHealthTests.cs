namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.3: compose must order cold starts by health, not by container
/// start. Every load-bearing service needs a healthcheck, dependents must
/// gate on service_healthy, and the images must ship the probe tooling.
/// </summary>
public class ComposeHealthTests
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
        var next = compose.IndexOf("\n  ", start + 1, StringComparison.Ordinal);
        // Next top-level service (two-space indent) or end of services.
        var scan = start + 1;
        while (true)
        {
            var candidate = compose.IndexOf("\n  ", scan, StringComparison.Ordinal);
            if (candidate < 0)
                return compose[start..];
            var after = compose.Substring(candidate + 1, 3);
            if (char.IsLetter(after[2]) && compose[candidate + 3] != ' ')
                return compose[start..candidate];
            scan = candidate + 1;
        }
    }

    public static IEnumerable<object[]> LoadBearingServices =>
        new List<object[]>
        {
            new object[] { "postgres" },
            new object[] { "redis" },
            new object[] { "rabbitmq" },
            new object[] { "identity-api" },
            new object[] { "merchant-api" },
            new object[] { "payment-api" },
            new object[] { "ledger-api" },
            new object[] { "notification-api" },
            new object[] { "settlement-api" },
            new object[] { "merchant-web" },
            new object[] { "admin-web" },
            new object[] { "nginx" },
        };

    [Theory]
    [MemberData(nameof(LoadBearingServices))]
    public void Service_DeclaresHealthcheck(string service)
    {
        Assert.Contains("healthcheck:", ServiceBlock(Read("docker-compose.yml"), service));
    }

    [Fact]
    public void Api_Healthchecks_HitReadiness()
    {
        var compose = Read("docker-compose.yml");

        foreach (var api in new[] { "identity-api", "merchant-api", "payment-api", "ledger-api", "notification-api", "settlement-api" })
            Assert.Contains("curl -f http://localhost:8080/health/ready", ServiceBlock(compose, api));
    }

    [Fact]
    public void Web_Healthchecks_HitWebHealthRoute()
    {
        var compose = Read("docker-compose.yml");

        Assert.Contains("/api/health", ServiceBlock(compose, "merchant-web"));
        Assert.Contains("/api/health", ServiceBlock(compose, "admin-web"));
    }

    [Fact]
    public void Nginx_Healthcheck_CoversProxiedUpstream()
    {
        Assert.Contains("/identity/health/live", ServiceBlock(Read("docker-compose.yml"), "nginx"));
    }

    [Fact]
    public void NoServiceStarted_GatesRemain()
    {
        // service_started proves nothing about readiness; every gate must be
        // service_healthy (Step 9.3). The webs/nginx previously had no
        // condition at all (short-form depends_on).
        Assert.DoesNotContain("service_started", Read("docker-compose.yml"));
        Assert.Contains("condition: service_healthy", Read("docker-compose.yml"));
    }

    [Fact]
    public void WebAndNginx_GateOnHealthyApis()
    {
        var compose = Read("docker-compose.yml");

        foreach (var dependent in new[] { "merchant-web", "admin-web", "nginx" })
        {
            var block = ServiceBlock(compose, dependent);
            foreach (var api in new[] { "identity-api", "merchant-api", "payment-api", "ledger-api", "notification-api", "settlement-api" })
                Assert.Contains(api, block);
            Assert.Contains("condition: service_healthy", block);
        }
    }

    [Theory]
    [InlineData("Identity")]
    [InlineData("Merchant")]
    [InlineData("Payment")]
    [InlineData("Ledger")]
    [InlineData("Notification")]
    [InlineData("Settlement")]
    public void Api_Dockerfile_ShipsCurlForHealthcheck(string service)
    {
        Assert.Contains("apt-get install -y --no-install-recommends curl",
            Read($"src/Services/{service}/{service}.API/Dockerfile"));
    }

    [Theory]
    [InlineData("merchant")]
    [InlineData("admin")]
    public void Web_HealthRouteExists(string app)
    {
        Assert.True(File.Exists(Path.Combine(RepoRoot(), $"apps/{app}/src/app/api/health/route.ts")));
    }
}
