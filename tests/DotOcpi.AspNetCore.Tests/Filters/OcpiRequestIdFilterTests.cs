using DotOcpi.AspNetCore;
using DotOcpi.AspNetCore.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

public class OcpiRequestIdMiddlewareTests
{
    private static OcpiRequestIdMiddleware CreateMiddleware(RequestDelegate? next = null)
    {
        return new OcpiRequestIdMiddleware(next ?? (_ => Task.CompletedTask));
    }

    [Fact]
    public async Task NoHeaders_UsesTraceIdentifier()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";

        await middleware.InvokeAsync(httpContext);

        httpContext.GetRequestId().Should().Be("trace-123");
    }

    [Fact]
    public async Task NoHeaders_UsesTraceIdentifierForCorrelationId()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-456";

        await middleware.InvokeAsync(httpContext);

        httpContext.GetCorrelationId().Should().Be("trace-456");
    }

    [Fact]
    public async Task ExistingRequestId_PreservesIt()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Request-ID"] = "existing-req-id";

        await middleware.InvokeAsync(httpContext);

        httpContext.GetRequestId().Should().Be("existing-req-id");
    }

    [Fact]
    public async Task ExistingCorrelationId_PreservesIt()
    {
        var middleware = CreateMiddleware();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Correlation-ID"] = "existing-corr-id";

        await middleware.InvokeAsync(httpContext);

        httpContext.GetCorrelationId().Should().Be("existing-corr-id");
    }

    [Fact]
    public async Task CallsNext()
    {
        var nextCalled = false;
        var middleware = CreateMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
    }
}
