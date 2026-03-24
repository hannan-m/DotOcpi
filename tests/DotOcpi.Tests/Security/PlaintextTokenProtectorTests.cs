using DotOcpi.Security;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Security;

[Trait("Category", "Security")]
public class PlaintextTokenProtectorTests
{
    private readonly PlaintextTokenProtector _protector = new();

    [Fact]
    public void Protect_ReturnsInputUnchanged()
    {
        var result = _protector.Protect("raw-token-value");

        result.Should().Be("raw-token-value");
    }

    [Fact]
    public void Unprotect_ReturnsInputUnchanged()
    {
        var result = _protector.Unprotect("raw-token-value");

        result.Should().Be("raw-token-value");
    }

    [Fact]
    public void RoundTrip_PreservesValue()
    {
        const string token = "cpo-token-abc123";

        var protectedValue = _protector.Protect(token);
        var unprotected = _protector.Unprotect(protectedValue);

        unprotected.Should().Be(token);
    }
}
