using BuildingBlocks.Shared.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using System.Net;
using HttpOverridesIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace BuildingBlocks.Shared.Tests;

public class ForwardedHeadersExtensionsTests
{
    private static IConfiguration BuildConfig(params (string Key, string Value)[] settings)
    {
        var data = settings.ToDictionary(s => s.Key, s => (string?)s.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void BuildForwardedHeadersOptions_Defaults_TrustLoopbackAndDockerBridge()
    {
        var options = ForwardedHeadersExtensions.BuildForwardedHeadersOptions(BuildConfig());

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Contains(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Parse("127.0.0.0")) && n.PrefixLength == 8);
        Assert.Contains(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Parse("172.16.0.0")) && n.PrefixLength == 12);
    }

    [Fact]
    public void BuildForwardedHeadersOptions_WithKnownNetworks_UsesConfiguredNetworks()
    {
        var options = ForwardedHeadersExtensions.BuildForwardedHeadersOptions(BuildConfig(
            ("ForwardedHeaders:KnownNetworks", "10.0.0.0/8,192.168.0.0/16")));

        Assert.Contains(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Parse("10.0.0.0")) && n.PrefixLength == 8);
        Assert.Contains(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Parse("192.168.0.0")) && n.PrefixLength == 16);
        Assert.DoesNotContain(options.KnownNetworks, n => n.Prefix.Equals(IPAddress.Parse("172.16.0.0")) && n.PrefixLength == 12);
    }

    [Fact]
    public void BuildForwardedHeadersOptions_WithKnownProxies_AddsProxies()
    {
        var options = ForwardedHeadersExtensions.BuildForwardedHeadersOptions(BuildConfig(
            ("ForwardedHeaders:KnownProxies", "10.0.0.1,10.0.0.2")));

        Assert.Contains(IPAddress.Parse("10.0.0.1"), options.KnownProxies);
        Assert.Contains(IPAddress.Parse("10.0.0.2"), options.KnownProxies);
    }
}