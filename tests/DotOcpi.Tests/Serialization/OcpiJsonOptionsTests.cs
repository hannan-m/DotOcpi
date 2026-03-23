using DotOcpi.Serialization;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Serialization;

[Trait("Category", "Security")]
public class OcpiJsonOptionsTests
{
    [Theory]
    [InlineData(OcpiVersion.V2_0)]
    [InlineData(OcpiVersion.V2_1_1)]
    [InlineData(OcpiVersion.V2_2)]
    [InlineData(OcpiVersion.V2_2_1)]
    public void GetOptions_MaxDepth_Is32(OcpiVersion version)
    {
        var options = OcpiJsonOptions.GetOptions(version);

        options.MaxDepth.Should().Be(32);
    }

    [Theory]
    [InlineData(OcpiVersion.V2_0)]
    [InlineData(OcpiVersion.V2_1_1)]
    [InlineData(OcpiVersion.V2_2)]
    [InlineData(OcpiVersion.V2_2_1)]
    public void GetOptions_IsReadOnly(OcpiVersion version)
    {
        var options = OcpiJsonOptions.GetOptions(version);

        options.IsReadOnly.Should().BeTrue();
    }
}
