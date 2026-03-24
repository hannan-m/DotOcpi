using System.Diagnostics.Metrics;
using DotOcpi.AspNetCore.Filters;
using DotOcpi.Observability;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

[Trait("Category", "Security")]
public class OcpiTokenAAuthFilterTests
{
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly OcpiTokenAAuthFilter _filter;

    public OcpiTokenAAuthFilterTests()
    {
        var tokenValidator = new OcpiTokenValidator(_tokenStore);
        var metrics = new OcpiMetrics(new TestMeterFactory());
        _filter = new OcpiTokenAAuthFilter(tokenValidator, metrics, new NullLogger<OcpiTokenAAuthFilter>());
    }

    private static DefaultHttpContext CreateHttpContextWithAuth(string? authHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authHeader is not null)
            httpContext.Request.Headers.Authorization = authHeader;
        return httpContext;
    }

    private static EndpointFilterInvocationContext CreateFilterContext(HttpContext httpContext)
    {
        var ctx = Substitute.For<EndpointFilterInvocationContext>();
        ctx.HttpContext.Returns(httpContext);
        return ctx;
    }

    [Fact]
    public async Task MissingAuthHeader_Returns401()
    {
        var httpContext = CreateHttpContextWithAuth();
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task MissingAuthHeader_DoesNotCallNext()
    {
        var httpContext = CreateHttpContextWithAuth();
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        await _filter.InvokeAsync(
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
    public async Task InvalidToken_Returns401()
    {
        _tokenStore.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TokenEntry?)null);

        var httpContext = CreateHttpContextWithAuth("Token unknown-token");
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task TokenB_Returns401()
    {
        var hash = TokenHasher.Hash("token-b-value");
        _tokenStore
            .FindAsync(hash, Arg.Any<CancellationToken>())
            .Returns(new TokenEntry(hash, TokenPurpose.TokenB, "NL:TNM"));

        var httpContext = CreateHttpContextWithAuth("Token token-b-value");
        var filterContext = CreateFilterContext(httpContext);
        var nextCalled = false;

        var result = await _filter.InvokeAsync(
            filterContext,
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>("next");
            }
        );

        result.Should().BeAssignableTo<IResult>();
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task ValidTokenA_CallsNext()
    {
        var hash = TokenHasher.Hash("token-a-value");
        _tokenStore
            .FindAsync(hash, Arg.Any<CancellationToken>())
            .Returns(new TokenEntry(hash, TokenPurpose.TokenA, "NL:TNM"));

        var httpContext = CreateHttpContextWithAuth("Token token-a-value");
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

    [Fact]
    public async Task ValidTokenA_StoresTokenEntryInHttpContext()
    {
        var hash = TokenHasher.Hash("token-a-value");
        var entry = new TokenEntry(hash, TokenPurpose.TokenA, "NL:TNM");
        _tokenStore.FindAsync(hash, Arg.Any<CancellationToken>()).Returns(entry);

        var httpContext = CreateHttpContextWithAuth("Token token-a-value");
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.Items[typeof(TokenEntry)].Should().BeSameAs(entry);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose() { }
    }
}
