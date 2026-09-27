using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using System.Net;
using HttpOverridesIPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace BuildingBlocks.Shared.Http;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Enables <c>X-Forwarded-Proto</c> / <c>X-Forwarded-For</c> processing so
    /// scheme-aware logic (Request.IsHttps, link building) is correct behind nginx.
    ///
    /// Trusted proxies/networks are read from config (<c>ForwardedHeaders:KnownProxies</c>
    /// and <c>ForwardedHeaders:KnownNetworks</c>). When neither is set, only loopback
    /// and the default Docker bridge subnet (172.16.0.0/12) are trusted — enough for
    /// the compose runtime without trusting arbitrary internet clients.
    /// </summary>
    public static IApplicationBuilder UsePaymentSwitchForwardedHeaders(this IApplicationBuilder app, IConfiguration configuration)
    {
        return app.UseForwardedHeaders(BuildForwardedHeadersOptions(configuration));
    }

    /// <summary>
    /// Builds the <see cref="ForwardedHeadersOptions"/> from config so tests can
    /// assert the trusted proxy/networks without hosting middleware.
    /// </summary>
    public static ForwardedHeadersOptions BuildForwardedHeadersOptions(IConfiguration configuration)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardedProtoHeaderName = "X-Forwarded-Proto",
            ForwardedForHeaderName = "X-Forwarded-For"
        };

        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();

        var knownNetworks = ReadList(configuration, "ForwardedHeaders:KnownNetworks");
        if (knownNetworks.Length > 0)
        {
            foreach (var cidr in knownNetworks)
            {
                options.KnownNetworks.Add(ParseNetwork(cidr));
            }
        }
        else
        {
            options.KnownNetworks.Add(new HttpOverridesIPNetwork(IPAddress.Parse("127.0.0.0"), 8));
            options.KnownNetworks.Add(new HttpOverridesIPNetwork(IPAddress.Parse("172.16.0.0"), 12));
        }

        var knownProxies = ReadList(configuration, "ForwardedHeaders:KnownProxies");
        foreach (var proxy in knownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }

        return options;
    }

    private static string[] ReadList(IConfiguration configuration, string section)
    {
        var values = configuration.GetSection(section).Get<string[]>();
        if (values is { Length: > 0 })
        {
            return values;
        }

        var raw = configuration[section];
        if (!string.IsNullOrWhiteSpace(raw))
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [];
    }

    private static HttpOverridesIPNetwork ParseNetwork(string cidr)
    {
        var parts = cidr.Split('/');
        var address = IPAddress.Parse(parts[0]);
        var prefixLength = parts.Length > 1 ? int.Parse(parts[1]) : 32;
        return new HttpOverridesIPNetwork(address, prefixLength);
    }
}