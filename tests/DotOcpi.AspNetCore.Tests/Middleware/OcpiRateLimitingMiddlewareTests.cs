using DotOcpi.AspNetCore.Middleware;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Middleware;

public class OcpiRateLimitingMiddlewareTests
{
    private static CpoConnection CreateConnection(string countryCode = "DE", string partyId = "ALL") =>
        new()
        {
            CpoCountryCode = countryCode,
            CpoPartyId = partyId,
            EmspCountryCode = "NL",
            EmspPartyId = "TNM",
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = new Dictionary<string, string>(),
            TokenBHash = "hash",
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    [Fact]
    public async Task NoConnection_PassesThrough()
    {
        var nextCalled = false;
        var middleware = new OcpiRateLimitingMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 1 }
        );

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(httpContext);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task UnderLimit_PassesThrough()
    {
        var callCount = 0;
        var middleware = new OcpiRateLimitingMiddleware(
            _ =>
            {
                callCount++;
                return Task.CompletedTask;
            },
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 5 }
        );

        var connection = CreateConnection();

        for (var i = 0; i < 5; i++)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            httpContext.Items[typeof(CpoConnection)] = connection;
            await middleware.InvokeAsync(httpContext);
        }

        callCount.Should().Be(5);
    }

    [Fact]
    public async Task OverLimit_Returns429()
    {
        var middleware = new OcpiRateLimitingMiddleware(
            _ => Task.CompletedTask,
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 2, Window = TimeSpan.FromMinutes(1) }
        );

        var connection = CreateConnection();

        // First two requests pass
        for (var i = 0; i < 2; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Response.Body = new MemoryStream();
            ctx.Items[typeof(CpoConnection)] = connection;
            await middleware.InvokeAsync(ctx);
        }

        // Third request should be rate limited
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.Items[typeof(CpoConnection)] = connection;
        await middleware.InvokeAsync(httpContext);

        httpContext.Response.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task OverLimit_IncludesRetryAfterHeader()
    {
        var middleware = new OcpiRateLimitingMiddleware(
            _ => Task.CompletedTask,
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 1, Window = TimeSpan.FromSeconds(30) }
        );

        var connection = CreateConnection();

        var ctx1 = new DefaultHttpContext();
        ctx1.Response.Body = new MemoryStream();
        ctx1.Items[typeof(CpoConnection)] = connection;
        await middleware.InvokeAsync(ctx1);

        var ctx2 = new DefaultHttpContext();
        ctx2.Response.Body = new MemoryStream();
        ctx2.Items[typeof(CpoConnection)] = connection;
        await middleware.InvokeAsync(ctx2);

        ctx2.Response.Headers.RetryAfter.ToString().Should().NotBeNullOrEmpty();
        int.Parse(ctx2.Response.Headers.RetryAfter.ToString(), System.Globalization.CultureInfo.InvariantCulture)
            .Should()
            .BeGreaterThan(0);
    }

    [Fact]
    public async Task DifferentCpos_HaveIndependentLimits()
    {
        var nextCount = 0;
        var middleware = new OcpiRateLimitingMiddleware(
            _ =>
            {
                nextCount++;
                return Task.CompletedTask;
            },
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 1 }
        );

        var conn1 = CreateConnection("DE", "CPO1");
        var conn2 = CreateConnection("NL", "CPO2");

        var ctx1 = new DefaultHttpContext();
        ctx1.Response.Body = new MemoryStream();
        ctx1.Items[typeof(CpoConnection)] = conn1;
        await middleware.InvokeAsync(ctx1);

        var ctx2 = new DefaultHttpContext();
        ctx2.Response.Body = new MemoryStream();
        ctx2.Items[typeof(CpoConnection)] = conn2;
        await middleware.InvokeAsync(ctx2);

        // Both should pass since they're different CPOs
        nextCount.Should().Be(2);
    }

    [Fact]
    public async Task OverLimit_DoesNotCallNext()
    {
        var nextCount = 0;
        var middleware = new OcpiRateLimitingMiddleware(
            _ =>
            {
                nextCount++;
                return Task.CompletedTask;
            },
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 1 }
        );

        var connection = CreateConnection();

        var ctx1 = new DefaultHttpContext();
        ctx1.Response.Body = new MemoryStream();
        ctx1.Items[typeof(CpoConnection)] = connection;
        await middleware.InvokeAsync(ctx1);

        var ctx2 = new DefaultHttpContext();
        ctx2.Response.Body = new MemoryStream();
        ctx2.Items[typeof(CpoConnection)] = connection;
        await middleware.InvokeAsync(ctx2);

        nextCount.Should().Be(1);
    }
}
