using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class OcpiVersionTests
{
    [Theory]
    [InlineData(OcpiVersion.V2_0, "2.0")]
    [InlineData(OcpiVersion.V2_1_1, "2.1.1")]
    [InlineData(OcpiVersion.V2_2, "2.2")]
    [InlineData(OcpiVersion.V2_2_1, "2.2.1")]
    public void ToVersionString_ReturnsCorrectWireFormat(OcpiVersion version, string expected)
    {
        version.ToVersionString().Should().Be(expected);
    }

    [Theory]
    [InlineData("2.0", OcpiVersion.V2_0)]
    [InlineData("2.1.1", OcpiVersion.V2_1_1)]
    [InlineData("2.2", OcpiVersion.V2_2)]
    [InlineData("2.2.1", OcpiVersion.V2_2_1)]
    public void TryParse_ValidString_ReturnsTrue(string input, OcpiVersion expected)
    {
        OcpiVersionExtensions.TryParse(input, out var result).Should().BeTrue();
        result.Should().Be(expected);
    }

    [Fact]
    public void TryParse_DeprecatedVersion21_MapsToV2_1_1()
    {
        OcpiVersionExtensions.TryParse("2.1", out var result).Should().BeTrue();
        result.Should().Be(OcpiVersion.V2_1_1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1.0")]
    [InlineData("2.3")]
    [InlineData("invalid")]
    public void TryParse_InvalidString_ReturnsFalse(string? input)
    {
        OcpiVersionExtensions.TryParse(input, out var result).Should().BeFalse();
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(OcpiVersion.V2_0, false)]
    [InlineData(OcpiVersion.V2_1_1, false)]
    [InlineData(OcpiVersion.V2_2, true)]
    [InlineData(OcpiVersion.V2_2_1, true)]
    public void UsesPartyIdInUrls_ReturnsCorrectly(OcpiVersion version, bool expected)
    {
        version.UsesPartyIdInUrls().Should().Be(expected);
    }

    [Fact]
    public void IsDeprecated_V2_2_ReturnsTrue()
    {
        OcpiVersion.V2_2.IsDeprecated().Should().BeTrue();
    }

    [Theory]
    [InlineData(OcpiVersion.V2_0)]
    [InlineData(OcpiVersion.V2_1_1)]
    [InlineData(OcpiVersion.V2_2_1)]
    public void IsDeprecated_NonDeprecatedVersions_ReturnsFalse(OcpiVersion version)
    {
        version.IsDeprecated().Should().BeFalse();
    }
}
