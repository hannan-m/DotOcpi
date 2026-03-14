using DotOcpi.Registration;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Registration;

public class VersionNegotiatorTests
{
    [Fact]
    public void Negotiate_SingleMutualVersion_ReturnsIt()
    {
        var result = VersionNegotiator.Negotiate(["2.2.1"]);

        result.Should().Be(OcpiVersion.V2_2_1);
    }

    [Fact]
    public void Negotiate_MultipleVersions_ReturnsHighest()
    {
        var result = VersionNegotiator.Negotiate(["2.0", "2.1.1", "2.2.1"]);

        result.Should().Be(OcpiVersion.V2_2_1);
    }

    [Fact]
    public void Negotiate_NoMutualVersion_ReturnsNull()
    {
        var result = VersionNegotiator.Negotiate(["3.0", "1.0"]);

        result.Should().BeNull();
    }

    [Fact]
    public void Negotiate_EmptyList_ReturnsNull()
    {
        var result = VersionNegotiator.Negotiate([]);

        result.Should().BeNull();
    }

    [Fact]
    public void Negotiate_Deprecated21_MapsTo211()
    {
        var result = VersionNegotiator.Negotiate(["2.1"]);

        result.Should().Be(OcpiVersion.V2_1_1);
    }

    [Fact]
    public void Negotiate_PrefersHigherVersion()
    {
        var result = VersionNegotiator.Negotiate(["2.0", "2.2"]);

        result.Should().Be(OcpiVersion.V2_2);
    }

    [Fact]
    public void Negotiate_WithSupportedVersionsFilter_RespectsFilter()
    {
        var supported = new HashSet<OcpiVersion> { OcpiVersion.V2_1_1 };

        var result = VersionNegotiator.Negotiate(["2.0", "2.1.1", "2.2.1"], supported);

        result.Should().Be(OcpiVersion.V2_1_1);
    }

    [Fact]
    public void Negotiate_FilterExcludesAll_ReturnsNull()
    {
        var supported = new HashSet<OcpiVersion> { OcpiVersion.V2_2_1 };

        var result = VersionNegotiator.Negotiate(["2.0"], supported);

        result.Should().BeNull();
    }

    [Fact]
    public void Negotiate_NullFilter_SupportsAll()
    {
        var result = VersionNegotiator.Negotiate(["2.0"], supportedVersions: null);

        result.Should().Be(OcpiVersion.V2_0);
    }

    [Fact]
    public void TryResolve_ValidVersions_ReturnsTrue()
    {
        VersionNegotiator.TryResolve("2.2.1", out var v1).Should().BeTrue();
        v1.Should().Be(OcpiVersion.V2_2_1);

        VersionNegotiator.TryResolve("2.2", out var v2).Should().BeTrue();
        v2.Should().Be(OcpiVersion.V2_2);

        VersionNegotiator.TryResolve("2.1.1", out var v3).Should().BeTrue();
        v3.Should().Be(OcpiVersion.V2_1_1);

        VersionNegotiator.TryResolve("2.0", out var v4).Should().BeTrue();
        v4.Should().Be(OcpiVersion.V2_0);
    }

    [Fact]
    public void TryResolve_UnknownVersion_ReturnsFalse()
    {
        VersionNegotiator.TryResolve("3.0", out _).Should().BeFalse();
    }

    [Fact]
    public void TryResolve_Deprecated21_MapsTo211()
    {
        VersionNegotiator.TryResolve("2.1", out var version).Should().BeTrue();
        version.Should().Be(OcpiVersion.V2_1_1);
    }
}
