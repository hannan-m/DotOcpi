using System.Net;
using DotOcpi.Client.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

[Trait("Category", "Security")]
public class SsrfGuardTests
{
    [Theory]
    [InlineData("0.0.0.0")] // "This network" — resolves to localhost on Linux
    [InlineData("0.1.2.3")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("100.64.0.1")] // CGNAT (RFC 6598)
    [InlineData("100.127.255.255")]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.255")]
    [InlineData("169.254.169.254")] // Cloud metadata
    [InlineData("169.254.1.1")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.0.0.1")] // IANA service continuity
    [InlineData("192.168.0.1")]
    [InlineData("192.168.255.255")]
    [InlineData("198.18.0.1")] // Benchmark testing
    [InlineData("198.19.255.255")]
    [InlineData("240.0.0.1")] // Reserved
    [InlineData("255.255.255.255")] // Broadcast
    public void IsBlockedAddress_BlocksPrivateIPv4(string ip)
    {
        SsrfGuard.IsBlockedAddress(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("100.63.255.255")] // Just below CGNAT range
    [InlineData("100.128.0.0")] // Just above CGNAT range
    [InlineData("172.15.255.255")] // Just below 172.16/12
    [InlineData("172.32.0.0")] // Just above 172.31
    [InlineData("192.0.1.0")] // Just above 192.0.0/24
    [InlineData("198.17.255.255")] // Just below 198.18
    [InlineData("198.20.0.0")] // Just above 198.19
    [InlineData("239.255.255.255")] // Just below 240/4
    public void IsBlockedAddress_AllowsPublicIPv4(string ip)
    {
        SsrfGuard.IsBlockedAddress(IPAddress.Parse(ip)).Should().BeFalse();
    }

    [Fact]
    public void IsBlockedAddress_BlocksIPv6Loopback()
    {
        SsrfGuard.IsBlockedAddress(IPAddress.IPv6Loopback).Should().BeTrue();
    }

    [Theory]
    [InlineData("fe80::1")] // Link-local
    [InlineData("fc00::1")] // ULA
    [InlineData("fd00::1")] // ULA
    public void IsBlockedAddress_BlocksPrivateIPv6(string ip)
    {
        SsrfGuard.IsBlockedAddress(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Theory]
    [InlineData("::ffff:127.0.0.1")] // IPv4-mapped loopback
    [InlineData("::ffff:10.0.0.1")] // IPv4-mapped private
    [InlineData("::ffff:0.0.0.0")] // IPv4-mapped "this network"
    public void IsBlockedAddress_BlocksIPv4MappedIPv6(string ip)
    {
        SsrfGuard.IsBlockedAddress(IPAddress.Parse(ip)).Should().BeTrue();
    }

    [Fact]
    public void IsBlockedAddress_AllowsPublicIPv6()
    {
        SsrfGuard.IsBlockedAddress(IPAddress.Parse("2001:4860:4860::8888")).Should().BeFalse();
    }

    [Fact]
    public void Validate_BlocksNon443Port()
    {
        var addresses = new[] { IPAddress.Parse("8.8.8.8") };

        var act = () => SsrfGuard.Validate(addresses, "example.com", 80);

        act.Should().Throw<InvalidOperationException>().WithMessage("*port 80*not allowed*");
    }

    [Fact]
    public void Validate_Allows443Port()
    {
        var addresses = new[] { IPAddress.Parse("8.8.8.8") };

        var act = () => SsrfGuard.Validate(addresses, "example.com", 443);

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_BlocksPrivateAddress()
    {
        var addresses = new[] { IPAddress.Parse("192.168.1.1") };

        var act = () => SsrfGuard.Validate(addresses, "internal.host", 443);

        act.Should().Throw<InvalidOperationException>().WithMessage("*SSRF blocked*");
    }
}
