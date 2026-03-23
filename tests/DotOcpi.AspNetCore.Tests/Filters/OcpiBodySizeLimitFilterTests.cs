using DotOcpi.AspNetCore.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

public class OcpiBodySizeLimitFilterTests
{
    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        var ctx = Substitute.For<EndpointFilterInvocationContext>();
        ctx.HttpContext.Returns(httpContext);
        return ctx;
    }

    [Fact]
    public async Task UnderLimit_CallsNext()
    {
        var filter = new OcpiBodySizeLimitFilter(1024);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = 500;
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task OverLimit_Returns413()
    {
        var filter = new OcpiBodySizeLimitFilter(1024);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = 2048;
        var filterContext = CreateFilterContext(httpContext);

        var result = await filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task OverLimit_DoesNotCallNext()
    {
        var filter = new OcpiBodySizeLimitFilter(1024);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = 2048;
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
    public async Task NoContentLength_CallsNext()
    {
        var filter = new OcpiBodySizeLimitFilter(1024);
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExactlyAtLimit_CallsNext()
    {
        var filter = new OcpiBodySizeLimitFilter(1024);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentLength = 1024;
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task NoContentLength_SetsKestrelBodySizeLimit()
    {
        var filter = new OcpiBodySizeLimitFilter(2048);
        var httpContext = new DefaultHttpContext();
        var bodySizeFeature = new FakeMaxRequestBodySizeFeature();
        httpContext.Features.Set<IHttpMaxRequestBodySizeFeature>(bodySizeFeature);
        var filterContext = CreateFilterContext(httpContext);

        await filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        bodySizeFeature.MaxRequestBodySize.Should().Be(2048);
    }

    private sealed class FakeMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public bool IsReadOnly => false;
        public long? MaxRequestBodySize { get; set; }
    }
}
