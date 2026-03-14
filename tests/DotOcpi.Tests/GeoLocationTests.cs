using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class GeoLocationTests
{
    [Fact]
    public void Properties_RetainStringValues()
    {
        var geo = new GeoLocation("51.0472", "3.7294");
        geo.Latitude.Should().Be("51.0472");
        geo.Longitude.Should().Be("3.7294");
    }

    [Fact]
    public void Equality_SameCoordinates_AreEqual()
    {
        var a = new GeoLocation("51.0472", "3.7294");
        var b = new GeoLocation("51.0472", "3.7294");
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentCoordinates_NotEqual()
    {
        var a = new GeoLocation("51.0472", "3.7294");
        var b = new GeoLocation("52.0000", "4.0000");
        a.Should().NotBe(b);
    }

    [Fact]
    public void PreservesExactPrecision()
    {
        var geo = new GeoLocation("51.047200000", "3.729400000");
        geo.Latitude.Should().Be("51.047200000");
    }
}
