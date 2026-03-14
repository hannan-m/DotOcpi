using DotOcpi.AspNetCore;
using DotOcpi.AspNetCore.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

public class OcpiRequestIdFilterTests
{
    private readonly OcpiRequestIdFilter _filter = new();

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        var ctx = Substitute.For<EndpointFilterInvocationContext>();
        ctx.HttpContext.Returns(httpContext);
        return ctx;
    }

    [Fact]
    public async Task NoHeaders_GeneratesRequestId()
    {
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.GetRequestId().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task NoHeaders_GeneratesCorrelationId()
    {
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.GetCorrelationId().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExistingRequestId_PreservesIt()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-ID"] = "existing-req-id";
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.GetRequestId().Should().Be("existing-req-id");
    }

    [Fact]
    public async Task ExistingCorrelationId_PreservesIt()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "existing-corr-id";
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.GetCorrelationId().Should().Be("existing-corr-id");
    }

    [Fact]
    public async Task EchoesRequestIdInResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-ID"] = "echo-me";
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.Response.Headers["X-Request-ID"].ToString().Should().Be("echo-me");
    }

    [Fact]
    public async Task EchoesCorrelationIdInResponse()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "corr-echo";
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.Response.Headers["X-Correlation-ID"].ToString().Should().Be("corr-echo");
    }

    [Fact]
    public async Task GeneratedIds_AreDifferent()
    {
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.GetRequestId().Should().NotBe(httpContext.GetCorrelationId());
    }

    [Fact]
    public async Task CallsNext()
    {
        var httpContext = new DefaultHttpContext();
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("ok");
            }
        );

        nextCalled.Should().BeTrue();
    }
}
