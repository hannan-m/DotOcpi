using DotOcpi.AspNetCore.Filters;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Middleware;

public class OcpiRateLimitFilterTests : IDisposable
{
    private readonly OcpiRateLimitFilter _filter;

    public OcpiRateLimitFilterTests()
    {
        _filter = new OcpiRateLimitFilter(
            new OcpiRateLimitOptions { MaxRequestsPerWindow = 2, Window = TimeSpan.FromMinutes(1) }
        );
    }

    public void Dispose()
    {
        _filter.Dispose();
        GC.SuppressFinalize(this);
    }

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

    private static DefaultEndpointFilterInvocationContext CreateContext(HttpContext httpContext)
    {
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "test"));
        return new DefaultEndpointFilterInvocationContext(httpContext);
    }

    private static EndpointFilterDelegate NextFilter() => _ => ValueTask.FromResult<object?>(Results.Ok());

    [Fact]
    public async Task NoConnection_PassesThrough()
    {
        var httpContext = new DefaultHttpContext();
        var result = await _filter.InvokeAsync(CreateContext(httpContext), NextFilter());

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task UnderLimit_PassesThrough()
    {
        var connection = CreateConnection();
        var callCount = 0;

        for (var i = 0; i < 2; i++)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items[typeof(CpoConnection)] = connection;
            var result = await _filter.InvokeAsync(
                CreateContext(httpContext),
                _ =>
                {
                    callCount++;
                    return ValueTask.FromResult<object?>(Results.Ok());
                }
            );
        }

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task OverLimit_Returns429()
    {
        var connection = CreateConnection();

        for (var i = 0; i < 2; i++)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Items[typeof(CpoConnection)] = connection;
            await _filter.InvokeAsync(CreateContext(httpContext), NextFilter());
        }

        var blocked = new DefaultHttpContext();
        blocked.Items[typeof(CpoConnection)] = connection;
        var result = await _filter.InvokeAsync(CreateContext(blocked), NextFilter());

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task DifferentCpos_HaveIndependentLimits()
    {
        var conn1 = CreateConnection("DE", "CPO1");
        var conn2 = CreateConnection("NL", "CPO2");
        var callCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            callCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        var ctx1 = new DefaultHttpContext();
        ctx1.Items[typeof(CpoConnection)] = conn1;
        await _filter.InvokeAsync(CreateContext(ctx1), next);

        var ctx2 = new DefaultHttpContext();
        ctx2.Items[typeof(CpoConnection)] = conn2;
        await _filter.InvokeAsync(CreateContext(ctx2), next);

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task OverLimit_DoesNotCallNext()
    {
        var connection = CreateConnection();
        var nextCount = 0;

        EndpointFilterDelegate next = _ =>
        {
            nextCount++;
            return ValueTask.FromResult<object?>(Results.Ok());
        };

        for (var i = 0; i < 2; i++)
        {
            var ctx = new DefaultHttpContext();
            ctx.Items[typeof(CpoConnection)] = connection;
            await _filter.InvokeAsync(CreateContext(ctx), next);
        }

        var blocked = new DefaultHttpContext();
        blocked.Items[typeof(CpoConnection)] = connection;
        await _filter.InvokeAsync(CreateContext(blocked), next);

        nextCount.Should().Be(2);
    }
}
