using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class OcpiSentinelTests
{
    [Fact]
    public void NotAvailable_HasCorrectValue()
    {
        OcpiSentinel.NotAvailable.Should().Be("#NA");
    }

    [Fact]
    public void IsNotAvailable_WithSentinel_ReturnsTrue()
    {
        OcpiSentinel.IsNotAvailable("#NA").Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#na")]
    [InlineData("NA")]
    [InlineData("some value")]
    public void IsNotAvailable_WithOtherValues_ReturnsFalse(string? value)
    {
        OcpiSentinel.IsNotAvailable(value).Should().BeFalse();
    }
}
