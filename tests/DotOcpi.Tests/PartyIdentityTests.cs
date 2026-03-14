using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests;

public class PartyIdentityTests
{
    [Fact]
    public void ToCompositeId_FormatsCorrectly()
    {
        var identity = new PartyIdentity("DE", "MSP");
        identity.ToCompositeId().Should().Be("DE_MSP");
    }

    [Fact]
    public void ToCompositeId_UppercasesInput()
    {
        var identity = new PartyIdentity("de", "msp");
        identity.ToCompositeId().Should().Be("DE_MSP");
    }

    [Fact]
    public void ToString_ReturnsCompositeId()
    {
        var identity = new PartyIdentity("NL", "CPO");
        identity.ToString().Should().Be("NL_CPO");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new PartyIdentity("DE", "MSP");
        var b = new PartyIdentity("DE", "MSP");
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        var a = new PartyIdentity("DE", "MSP");
        var b = new PartyIdentity("NL", "CPO");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_CaseSensitive()
    {
        // PartyIdentity uses default record struct equality (case-sensitive)
        // ToCompositeId normalizes, but raw values are compared as-is
        var a = new PartyIdentity("DE", "MSP");
        var b = new PartyIdentity("de", "msp");
        a.Should().NotBe(b);
    }

    [Fact]
    public void Properties_RetainValues()
    {
        var identity = new PartyIdentity("US", "ABC");
        identity.CountryCode.Should().Be("US");
        identity.PartyId.Should().Be("ABC");
    }
}
