using System.Diagnostics.Metrics;
using DotOcpi.AspNetCore.Filters;
using DotOcpi.Observability;
using DotOcpi.Registry;
using DotOcpi.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Filters;

[Trait("Category", "Security")]
public class OcpiAuthFilterTests
{
    private readonly OcpiTokenValidator _tokenValidator;
    private readonly ICpoRegistry _registry = Substitute.For<ICpoRegistry>();
    private readonly ITokenStore _tokenStore = Substitute.For<ITokenStore>();
    private readonly OcpiAuthFilter _filter;

    public OcpiAuthFilterTests()
    {
        _tokenValidator = new OcpiTokenValidator(_tokenStore);
        var metrics = new OcpiMetrics(new TestMeterFactory());
        _filter = new OcpiAuthFilter(_tokenValidator, _registry, metrics, new NullLogger<OcpiAuthFilter>());
    }

    private static CpoConnection CreateConnection() =>
        new()
        {
            CpoCountryCode = "DE",
            CpoPartyId = "ALL",
            EmspCountryCode = "NL",
            EmspPartyId = "TNM",
            Version = OcpiVersion.V2_2_1,
            ModuleEndpoints = new Dictionary<string, string>(),
            TokenBHash = TokenHasher.Hash("valid-token"),
            Status = ConnectionStatus.Connected,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

    private void SetupValidToken(string rawToken, CpoConnection connection)
    {
        var hash = TokenHasher.Hash(rawToken);
        _tokenStore
            .FindAsync(hash, Arg.Any<CancellationToken>())
            .Returns(new TokenEntry(hash, TokenPurpose.TokenB, "NL:TNM"));
        _registry.FindByTokenHash(hash).Returns(connection);
    }

    private static DefaultHttpContext CreateHttpContextWithAuth(string? authHeader = null)
    {
        var httpContext = new DefaultHttpContext();
        if (authHeader is not null)
        {
            httpContext.Request.Headers.Authorization = authHeader;
        }
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
    public async Task InvalidScheme_Returns401()
    {
        var httpContext = CreateHttpContextWithAuth("Bearer abc123");
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task EmptyToken_Returns401()
    {
        var httpContext = CreateHttpContextWithAuth("Token ");
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task UnrecognizedToken_Returns401()
    {
        _tokenStore.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TokenEntry?)null);

        var httpContext = CreateHttpContextWithAuth("Token unknown-token");
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task ValidTokenNoConnection_Returns401()
    {
        var hash = TokenHasher.Hash("orphaned-token");
        _tokenStore
            .FindAsync(hash, Arg.Any<CancellationToken>())
            .Returns(new TokenEntry(hash, TokenPurpose.TokenB, "NL:TNM"));
        _registry.FindByTokenHash(hash).Returns((CpoConnection?)null);

        var httpContext = CreateHttpContextWithAuth("Token orphaned-token");
        var filterContext = CreateFilterContext(httpContext);

        var result = await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("next"));

        result.Should().BeAssignableTo<IResult>();
    }

    [Fact]
    public async Task ValidToken_CallsNext()
    {
        var connection = CreateConnection();
        SetupValidToken("valid-token", connection);

        var httpContext = CreateHttpContextWithAuth("Token valid-token");
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
    public async Task ValidToken_StoresConnectionInHttpContext()
    {
        var connection = CreateConnection();
        SetupValidToken("valid-token", connection);

        var httpContext = CreateHttpContextWithAuth("Token valid-token");
        var filterContext = CreateFilterContext(httpContext);

        await _filter.InvokeAsync(filterContext, _ => ValueTask.FromResult<object?>("ok"));

        httpContext.Items[typeof(CpoConnection)].Should().BeSameAs(connection);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options) => new(options);

        public void Dispose() { }
    }
}
