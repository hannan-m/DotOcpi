using DotOcpi.AspNetCore.Routing;
using FluentAssertions;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Routing;

public class OcpiEndpointRouteBuilderExtensionsTests
{
    [Theory]
    [InlineData(OcpiVersion.V2_0, true, "{objectId}")]
    [InlineData(OcpiVersion.V2_0, false, "")]
    [InlineData(OcpiVersion.V2_1_1, true, "{objectId}")]
    [InlineData(OcpiVersion.V2_1_1, false, "")]
    [InlineData(OcpiVersion.V2_2, true, "{countryCode}/{partyId}/{objectId}")]
    [InlineData(OcpiVersion.V2_2, false, "{countryCode}/{partyId}")]
    [InlineData(OcpiVersion.V2_2_1, true, "{countryCode}/{partyId}/{objectId}")]
    [InlineData(OcpiVersion.V2_2_1, false, "{countryCode}/{partyId}")]
    public void BuildPattern_ReturnsCorrectPattern(OcpiVersion version, bool withObjectId, string expected)
    {
        var pattern = OcpiEndpointRouteBuilderExtensions.BuildPattern(version, withObjectId);

        pattern.Should().Be(expected);
    }
}
