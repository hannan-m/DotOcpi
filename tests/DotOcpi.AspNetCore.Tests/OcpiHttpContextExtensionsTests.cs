using DotOcpi.AspNetCore;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.AspNetCore.Tests;

public class OcpiHttpContextExtensionsTests
{
    private static DefaultHttpContext CreateHttpContext() => new();

    private static OcpiRequestContext CreateOcpiContext() =>
        new()
        {
            Connection = new CpoConnection
            {
                CpoCountryCode = "DE",
                CpoPartyId = "ALL",
                EmspCountryCode = "NL",
                EmspPartyId = "TNM",
                Version = OcpiVersion.V2_2_1,
                ModuleEndpoints = new Dictionary<string, string>(),
                TokenBHash = "hash",
                Status = ConnectionStatus.Connected,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
            },
            RequestId = "req-123",
            CorrelationId = "corr-456",
            CpoId = "DE_ALL",
            CpoIdentity = new PartyIdentity("DE", "ALL"),
            EmspIdentity = new PartyIdentity("NL", "TNM"),
            NegotiatedVersion = OcpiVersion.V2_2_1,
            ModuleId = "locations",
        };

    [Fact]
    public void GetOcpiContext_NotSet_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        httpContext.GetOcpiContext().Should().BeNull();
    }

    [Fact]
    public void SetAndGetOcpiContext_RoundTrips()
    {
        var httpContext = CreateHttpContext();
        var context = CreateOcpiContext();

        httpContext.SetOcpiContext(context);

        httpContext.GetOcpiContext().Should().BeSameAs(context);
    }

    [Fact]
    public void GetRequestId_NotSet_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        httpContext.GetRequestId().Should().BeNull();
    }

    [Fact]
    public void SetAndGetRequestId_RoundTrips()
    {
        var httpContext = CreateHttpContext();

        httpContext.SetRequestId("req-abc");

        httpContext.GetRequestId().Should().Be("req-abc");
    }

    [Fact]
    public void GetCorrelationId_NotSet_ReturnsNull()
    {
        var httpContext = CreateHttpContext();

        httpContext.GetCorrelationId().Should().BeNull();
    }

    [Fact]
    public void SetAndGetCorrelationId_RoundTrips()
    {
        var httpContext = CreateHttpContext();

        httpContext.SetCorrelationId("corr-xyz");

        httpContext.GetCorrelationId().Should().Be("corr-xyz");
    }

    [Fact]
    public void MultipleContexts_AreIndependent()
    {
        var ctx1 = CreateHttpContext();
        var ctx2 = CreateHttpContext();

        ctx1.SetRequestId("req-1");
        ctx2.SetRequestId("req-2");

        ctx1.GetRequestId().Should().Be("req-1");
        ctx2.GetRequestId().Should().Be("req-2");
    }
}
