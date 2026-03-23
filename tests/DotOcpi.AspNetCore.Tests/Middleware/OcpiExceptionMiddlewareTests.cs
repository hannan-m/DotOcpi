using System.Text.Json;
using DotOcpi.AspNetCore.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Middleware;

public class OcpiExceptionMiddlewareTests
{
    private static ILogger<OcpiExceptionMiddleware> CreateLogger() =>
        NullLoggerFactory.Instance.CreateLogger<OcpiExceptionMiddleware>();

    [Fact]
    public async Task NoException_PassesThrough()
    {
        var nextCalled = false;
        var middleware = new OcpiExceptionMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            CreateLogger()
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task UnhandledException_Returns500()
    {
        var middleware = new OcpiExceptionMiddleware(
            _ => throw new InvalidOperationException("test error"),
            CreateLogger()
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task UnhandledException_ReturnsOcpiStatus3000()
    {
        var middleware = new OcpiExceptionMiddleware(
            _ => throw new InvalidOperationException("test error"),
            CreateLogger()
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        doc.RootElement.GetProperty("status_code").GetInt32().Should().Be(3000);
    }

    [Fact]
    [Trait("Category", "Security")]
    public async Task UnhandledException_DoesNotLeakStackTrace()
    {
        var middleware = new OcpiExceptionMiddleware(
            _ => throw new InvalidOperationException("secret details about the system"),
            CreateLogger()
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().NotContain("secret details");
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task UnhandledException_SetsJsonContentType()
    {
        var middleware = new OcpiExceptionMiddleware(_ => throw new InvalidOperationException("test"), CreateLogger());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task CancelledRequest_DoesNotReturn500()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var middleware = new OcpiExceptionMiddleware(
            _ => throw new OperationCanceledException(cts.Token),
            CreateLogger()
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestAborted = cts.Token;

        await middleware.InvokeAsync(httpContext);

        // Default status code, not 500
        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UnhandledException_IncludesTimestamp()
    {
        var middleware = new OcpiExceptionMiddleware(_ => throw new InvalidOperationException("test"), CreateLogger());

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(httpContext.Response.Body);
        doc.RootElement.TryGetProperty("timestamp", out _).Should().BeTrue();
    }
}
