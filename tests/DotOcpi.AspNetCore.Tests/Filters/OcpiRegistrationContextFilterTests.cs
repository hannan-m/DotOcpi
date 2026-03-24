using DotOcpi.AspNetCore.Filters;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

public class OcpiRegistrationContextFilterTests
{
    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        var ctx = Substitute.For<EndpointFilterInvocationContext>();
        ctx.HttpContext.Returns(httpContext);
        return ctx;
    }

    [Fact]
    public async Task MissingTokenEntry_Returns500()
    {
        var filter = new OcpiRegistrationContextFilter(OcpiVersion.V2_2_1);
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task MissingTokenEntry_DoesNotCallNext()
    {
        var filter = new OcpiRegistrationContextFilter(OcpiVersion.V2_2_1);
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("next");
            }
        );

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidTokenEntry_BuildsRegistrationContext()
    {
        var filter = new OcpiRegistrationContextFilter(OcpiVersion.V2_2_1);
        var entry = new TokenEntry("hash", TokenPurpose.TokenA, "NL:TNM");
        var httpContext = new DefaultHttpContext();
        httpContext.Items[typeof(TokenEntry)] = entry;
        httpContext.SetRequestId("req-123");
        httpContext.SetCorrelationId("corr-456");
        var filterContext = CreateFilterContext(httpContext);

        await filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        var ctx = httpContext.GetRegistrationContext();
        ctx.Should().NotBeNull();
        ctx!.Version.Should().Be(OcpiVersion.V2_2_1);
        ctx.TokenAEntry.Should().BeSameAs(entry);
        ctx.RequestId.Should().Be("req-123");
        ctx.CorrelationId.Should().Be("corr-456");
    }

    [Theory]
    [InlineData(OcpiVersion.V2_0)]
    [InlineData(OcpiVersion.V2_1_1)]
    [InlineData(OcpiVersion.V2_2)]
    [InlineData(OcpiVersion.V2_2_1)]
    public async Task RegistrationContext_HasCorrectVersion(OcpiVersion version)
    {
        var filter = new OcpiRegistrationContextFilter(version);
        var entry = new TokenEntry("hash", TokenPurpose.TokenA, "NL:TNM");
        var httpContext = new DefaultHttpContext();
        httpContext.Items[typeof(TokenEntry)] = entry;
        var filterContext = CreateFilterContext(httpContext);

        await filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        var ctx = httpContext.GetRegistrationContext();
        ctx.Should().NotBeNull();
        ctx!.Version.Should().Be(version);
    }
}
