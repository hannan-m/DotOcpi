using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class OcpiStatusCodeTests
{
    [Fact]
    public void Success_IsSuccess_ReturnsTrue()
    {
        OcpiStatusCode.Success.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Success_IsNotClientOrServerError()
    {
        OcpiStatusCode.Success.IsClientError.Should().BeFalse();
        OcpiStatusCode.Success.IsServerError.Should().BeFalse();
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2001)]
    [InlineData(2002)]
    [InlineData(2003)]
    [InlineData(2004)]
    [InlineData(2999)]
    public void ClientErrors_IsClientError_ReturnsTrue(int code)
    {
        new OcpiStatusCode(code).IsClientError.Should().BeTrue();
    }

    [Theory]
    [InlineData(3000)]
    [InlineData(3001)]
    [InlineData(3002)]
    [InlineData(3003)]
    [InlineData(3999)]
    public void ServerErrors_IsServerError_ReturnsTrue(int code)
    {
        new OcpiStatusCode(code).IsServerError.Should().BeTrue();
    }

    [Fact]
    public void KnownCodes_HaveCorrectValues()
    {
        OcpiStatusCode.Success.Value.Should().Be(1000);
        OcpiStatusCode.GenericClientError.Value.Should().Be(2000);
        OcpiStatusCode.InvalidParameters.Value.Should().Be(2001);
        OcpiStatusCode.NotEnoughInformation.Value.Should().Be(2002);
        OcpiStatusCode.UnknownLocation.Value.Should().Be(2003);
        OcpiStatusCode.UnknownToken.Value.Should().Be(2004);
        OcpiStatusCode.GenericServerError.Value.Should().Be(3000);
        OcpiStatusCode.UnableToUseClientApi.Value.Should().Be(3001);
        OcpiStatusCode.UnsupportedVersion.Value.Should().Be(3002);
        OcpiStatusCode.NoMatchingEndpoints.Value.Should().Be(3003);
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        new OcpiStatusCode(1000).Should().Be(OcpiStatusCode.Success);
    }

    [Fact]
    public void ToString_ReturnsNumericValue()
    {
        OcpiStatusCode.Success.ToString().Should().Be("1000");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    [InlineData(4000)]
    public void OutOfRange_NoCategoryMatches(int code)
    {
        var status = new OcpiStatusCode(code);
        status.IsSuccess.Should().BeFalse();
        status.IsClientError.Should().BeFalse();
        status.IsServerError.Should().BeFalse();
    }
}
