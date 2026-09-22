namespace BuildingBlocks.Shared.Tests;

/// <summary>
/// Step 9.9: nginx must re-resolve upstreams at request time. A static
/// `proxy_pass http://nameJulia/...` resolves once at startup, so recreating any
/// API container silently breaks its route until nginx restarts. Every
/// proxy target therefore goes through a variable (runtime DNS via the
/// Docker embedded resolver), with an explicit rewrite preserving the
/// prefix-strip the static URI form performed.
/// </summary>
public class NginxDnsTests
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

    private static readonly string[] Confs =
        ["infra/nginx/http/server.conf", "infra/nginx/tls/server.conf"];

    private static readonly (string Prefix, string Host, string Port, string Variable)[] ApiRoutes =
    [
        ("identity", "identity-api", "8080", "identity_upstream"),
        ("merchant", "merchant-api", "8080", "merchant_api_upstream"),
        ("payment", "payment-api", "8080", "payment_upstream"),
        ("ledger", "ledger-api", "8080", "ledger_upstream"),
        ("notification", "notification-api", "8080", "notification_upstream"),
        ("settlement", "settlement-api", "8080", "settlement_upstream"),
    ];

    [Theory]
    [InlineData("infra/nginx/http/server.conf")]
    [InlineData("infra/nginx/tls/server.conf")]
    public void Resolver_ConfiguredForRuntimeDns(string conf)
    {
        Assert.Contains("resolver 127.0.0.11", Read(conf));
    }

    [Theory]
    [InlineData("infra/nginx/http/server.conf")]
    [InlineData("infra/nginx/tls/server.conf")]
    public void NoStaticUpstreamProxyPass(string conf)
    {
        // Every backend proxy_pass must go through a variable; a static
        // hostname is resolved once at startup and cached indefinitely.
        foreach (var line in Read(conf).Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("proxy_pass http://", StringComparison.Ordinal))
                continue;
            Assert.Contains("$", trimmed);
        }
    }

    [Theory]
    [InlineData("infra/nginx/http/server.conf")]
    [InlineData("infra/nginx/tls/server.conf")]
    public void ApiRoutes_StripPrefixViaRewrite(string conf)
    {
        // Variables disable proxy_pass URI replacement, so each API location
        // needs its own rewrite (proven against real nginx: /identity/a?x=1
        // reaches upstream as /a?x=1).
        var text = Read(conf);
        foreach (var (prefix, host, port, variable) in ApiRoutes)
        {
            Assert.Contains($"set ${variable} {host};", text);
            Assert.Contains($"rewrite ^/{prefix}/(.*)$ /$1 break;", text);
            Assert.Contains($"proxy_pass http://${variable}:{port};", text);
        }
    }

    [Theory]
    [InlineData("infra/nginx/http/server.conf")]
    [InlineData("infra/nginx/tls/server.conf")]
    public void WebRoutes_PassThroughUnchanged(string conf)
    {
        var text = Read(conf);
        Assert.Contains("proxy_pass http://$admin_upstream:3001;", text);
        Assert.Contains("proxy_pass http://$merchant_upstream:3000;", text);
    }

    [Theory]
    [InlineData("infra/nginx/http/server.conf")]
    [InlineData("infra/nginx/tls/server.conf")]
    public void DenyLocation_StillBlocksDiscoverySurfaces(string conf)
    {
        Assert.Contains("metrics|hangfire", Read(conf));
    }
}
