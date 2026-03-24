using DotOcpi.AspNetCore.Security;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Security;

[Trait("Category", "Security")]
public class DataProtectionTokenProtectorTests
{
    private readonly DataProtectionTokenProtector _protector;

    public DataProtectionTokenProtectorTests()
    {
        var provider = DataProtectionProvider.Create("DotOcpi.Tests");
        _protector = new DataProtectionTokenProtector(provider);
    }

    [Fact]
    public void Protect_ReturnsDifferentValue()
    {
        var protectedValue = _protector.Protect("raw-token");

        protectedValue.Should().NotBe("raw-token");
    }

    [Fact]
    public void RoundTrip_PreservesValue()
    {
        const string token = "cpo-token-abc123-xyz";

        var protectedValue = _protector.Protect(token);
        var unprotected = _protector.Unprotect(protectedValue);

        unprotected.Should().Be(token);
    }

    [Fact]
    public void Protect_ProducesDifferentOutputPerCall()
    {
        var a = _protector.Protect("same-token");
        var b = _protector.Protect("same-token");

        // Data Protection uses unique nonces per call
        a.Should().NotBe(b);
    }

    [Fact]
    public void Unprotect_TamperedData_Throws()
    {
        var protectedValue = _protector.Protect("raw-token");
        var tampered = protectedValue + "X";

        var act = () => _protector.Unprotect(tampered);

        act.Should().Throw<Exception>();
    }
}
