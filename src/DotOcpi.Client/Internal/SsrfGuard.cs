using System.Net;
using System.Net.Sockets;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Validates resolved IP addresses against private, loopback, and link-local
/// ranges to prevent SSRF attacks via CPO-provided endpoint URLs.
/// </summary>
internal static class SsrfGuard
{
    /// <summary>
    /// Returns true if the address is in a blocked range (private, loopback,
    /// link-local, or the cloud metadata endpoint).
    /// </summary>
    internal static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
            return true;

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal) // fe80::/10
                return true;
            if (address.IsIPv6SiteLocal) // fec0::/10 (deprecated but still block)
                return true;

            // fc00::/7 — Unique Local Addresses
            var bytes = address.GetAddressBytes();
            if ((bytes[0] & 0xFE) == 0xFC)
                return true;

            // Map IPv6-mapped IPv4 to check IPv4 ranges
            if (address.IsIPv4MappedToIPv6)
                return IsBlockedAddress(address.MapToIPv4());

            return false;
        }

        // IPv4 ranges
        var ipBytes = address.GetAddressBytes();
        return ipBytes[0] switch
        {
            0 => true, // 0.0.0.0/8 — "this network", resolves to localhost on many platforms
            10 => true, // 10.0.0.0/8
            100 when ipBytes[1] >= 64 && ipBytes[1] <= 127 => true, // 100.64.0.0/10 — CGNAT (RFC 6598)
            127 => true, // 127.0.0.0/8 (redundant with IsLoopback, defense-in-depth)
            169 when ipBytes[1] == 254 => true, // 169.254.0.0/16 (link-local + cloud metadata 169.254.169.254)
            172 when ipBytes[1] >= 16 && ipBytes[1] <= 31 => true, // 172.16.0.0/12
            192 when ipBytes[1] == 0 && ipBytes[2] == 0 => true, // 192.0.0.0/24 — IANA service continuity (RFC 7534)
            192 when ipBytes[1] == 168 => true, // 192.168.0.0/16
            198 when ipBytes[1] >= 18 && ipBytes[1] <= 19 => true, // 198.18.0.0/15 — benchmark testing (RFC 2544)
            >= 240 => true, // 240.0.0.0/4 — reserved + 255.255.255.255 broadcast
            _ => false,
        };
    }

    /// <summary>
    /// Validates that a URI targets an allowed host:port. Throws if the
    /// resolved address is in a blocked range or the port is not allowed.
    /// </summary>
    /// <param name="addresses">DNS-resolved addresses for the host.</param>
    /// <param name="host">The original hostname (for error messages).</param>
    /// <param name="port">The target port.</param>
    /// <param name="allowedPorts">Allowed ports. Defaults to 443 only.</param>
    /// <exception cref="InvalidOperationException">Thrown when the address or port is blocked.</exception>
    internal static void Validate(
        ReadOnlySpan<IPAddress> addresses,
        string host,
        int port,
        IReadOnlySet<int>? allowedPorts = null
    )
    {
        allowedPorts ??= DefaultAllowedPorts;

        if (!allowedPorts.Contains(port))
        {
            throw new InvalidOperationException(
                $"SSRF blocked: port {port} is not allowed for OCPI requests to '{host}'. Allowed: {string.Join(", ", allowedPorts)}."
            );
        }

        foreach (var address in addresses)
        {
            if (IsBlockedAddress(address))
            {
                throw new InvalidOperationException(
                    $"SSRF blocked: resolved address {address} for host '{host}' is in a private/loopback/link-local range."
                );
            }
        }
    }

    private static readonly HashSet<int> DefaultAllowedPorts = [443];
}
