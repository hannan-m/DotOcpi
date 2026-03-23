using System.Net;

namespace DotOcpi.Simulator.Infrastructure;

/// <summary>
/// Validates outbound URLs against private/loopback/link-local addresses
/// to prevent SSRF even in test environments.
/// </summary>
internal static class SsrfGuard
{
    /// <summary>
    /// Returns true if the URL resolves to a publicly routable address.
    /// Returns false for private, loopback, link-local, and metadata addresses.
    /// </summary>
    public static bool IsUrlSafeForOutbound(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https"))
            return false;

        try
        {
            var addresses = Dns.GetHostAddresses(uri.Host);
            foreach (var addr in addresses)
            {
                if (!IsAddressSafe(addr))
                    return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }

    private static bool IsAddressSafe(IPAddress addr)
    {
        if (IPAddress.IsLoopback(addr))
            return false;

        if (addr.IsIPv6LinkLocal)
            return false;

        var bytes = addr.GetAddressBytes();

        if (bytes.Length == 4)
            return IsIpv4Safe(bytes);

        if (bytes.Length == 16)
        {
            // fc00::/7 covers both fc00:: and fd00:: (unique local addresses)
            if ((bytes[0] & 0xfe) == 0xfc)
                return false;

            // IPv4-mapped IPv6 (::ffff:x.x.x.x)
            if (addr.IsIPv4MappedToIPv6)
            {
                var mapped = addr.MapToIPv4();
                if (IPAddress.IsLoopback(mapped))
                    return false;

                return IsIpv4Safe(mapped.GetAddressBytes());
            }
        }

        return true;
    }

    private static bool IsIpv4Safe(byte[] bytes)
    {
        if (bytes[0] == 0)
            return false; // 0.0.0.0/8 — "this network", resolves to localhost on many platforms
        if (bytes[0] == 10)
            return false; // 10.0.0.0/8
        if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127)
            return false; // 100.64.0.0/10 — CGNAT (RFC 6598)
        if (bytes[0] == 127)
            return false; // 127.0.0.0/8
        if (bytes[0] == 169 && bytes[1] == 254)
            return false; // 169.254.0.0/16 (link-local + metadata)
        if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            return false; // 172.16.0.0/12
        if (bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
            return false; // 192.0.0.0/24 — IANA service continuity (RFC 7534)
        if (bytes[0] == 192 && bytes[1] == 168)
            return false; // 192.168.0.0/16
        if (bytes[0] == 198 && bytes[1] >= 18 && bytes[1] <= 19)
            return false; // 198.18.0.0/15 — benchmark testing (RFC 2544)
        if (bytes[0] >= 240)
            return false; // 240.0.0.0/4 — reserved + 255.255.255.255 broadcast
        return true;
    }
}
