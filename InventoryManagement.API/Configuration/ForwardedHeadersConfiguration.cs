using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace InventoryManagement.API.Configuration
{
    public static class ForwardedHeadersConfiguration
    {
        public const string KnownProxiesName = "ReverseProxy:KnownProxies";
        public const string KnownNetworksName = "ReverseProxy:KnownNetworks";

        public static void Configure(
            ForwardedHeadersOptions options,
            IConfiguration configuration)
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;

            foreach (var proxy in GetConfiguredValues(configuration, KnownProxiesName))
            {
                if (IPAddress.TryParse(proxy, out var address))
                {
                    options.KnownProxies.Add(address);
                }
            }

            foreach (var network in GetConfiguredValues(configuration, KnownNetworksName))
            {
                if (TryParseNetwork(network, out var prefix, out var prefixLength))
                {
                    options.KnownNetworks.Add(
                        new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
                            prefix,
                            prefixLength));
                }
            }
        }

        public static IReadOnlyCollection<string> ValidateProductionConfiguration(
            IConfiguration configuration)
        {
            var errors = new List<string>();
            var proxies = GetConfiguredValues(configuration, KnownProxiesName);
            var networks = GetConfiguredValues(configuration, KnownNetworksName);

            if (proxies.Length == 0 && networks.Length == 0)
            {
                errors.Add(
                    $"{KnownProxiesName} or {KnownNetworksName} must contain at least one trusted proxy or network.");
            }

            foreach (var proxy in proxies)
            {
                if (!IPAddress.TryParse(proxy, out _))
                {
                    errors.Add($"{KnownProxiesName} contains an invalid IP address.");
                }
            }

            foreach (var network in networks)
            {
                if (!TryParseNetwork(network, out _, out _))
                {
                    errors.Add($"{KnownNetworksName} contains an invalid CIDR network.");
                }
            }

            return errors;
        }

        private static string[] GetConfiguredValues(
            IConfiguration configuration,
            string sectionName)
        {
            return configuration
                .GetSection(sectionName)
                .Get<string[]>()?
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToArray() ?? [];
        }

        private static bool TryParseNetwork(
            string network,
            out IPAddress prefix,
            out int prefixLength)
        {
            prefix = IPAddress.None;
            prefixLength = 0;

            var parts = network.Split('/', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 ||
                !IPAddress.TryParse(parts[0], out var parsedPrefix) ||
                !int.TryParse(parts[1], out prefixLength))
            {
                return false;
            }

            prefix = parsedPrefix;
            var maxPrefixLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                ? 32
                : 128;

            return prefixLength >= 0 && prefixLength <= maxPrefixLength;
        }
    }
}
