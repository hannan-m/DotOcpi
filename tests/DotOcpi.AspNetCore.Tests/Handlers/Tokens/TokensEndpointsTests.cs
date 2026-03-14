using System.Text;
using DotOcpi.AspNetCore.Handlers.Tokens;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Tokens;

public class TokensEndpointsTests
{
    private static DefaultHttpContext CreateTokensGetContext(
        ITokensSender sender,
        OcpiVersion version,
        string? queryString = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(sender).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version, "tokens"));
        httpContext.Request.Path = $"/ocpi/{version.ToVersionString()}/tokens";

        if (queryString is not null)
        {
            httpContext.Request.QueryString = new QueryString(queryString);
        }

        return httpContext;
    }

    private static DefaultHttpContext CreateAuthorizeContext(
        ITokensAuthorizer authorizer,
        OcpiVersion version,
        string? body = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(authorizer).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version, "tokens"));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    [Fact]
    public async Task HandleTokensGet_ReturnsPaginationHeaders()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = new object[] { "token1", "token2" },
                    TotalCount = 10,
                    Offset = 0,
                    Limit = 2,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1, "?offset=0&limit=2");

        await TokensEndpoints.HandleTokensGet(httpContext);

        httpContext.Response.Headers["X-Total-Count"].ToString().Should().Be("10");
        httpContext.Response.Headers["X-Limit"].ToString().Should().Be("2");
        httpContext.Response.Headers["Link"].ToString().Should().Contain("offset=2");
    }

    [Fact]
    public async Task HandleTokensGet_NoLinkHeader_WhenLastPage()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = new object[] { "token1" },
                    TotalCount = 3,
                    Offset = 2,
                    Limit = 2,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1, "?offset=2&limit=2");

        await TokensEndpoints.HandleTokensGet(httpContext);

        httpContext.Response.Headers.ContainsKey("Link").Should().BeFalse();
    }

    [Fact]
    public async Task HandleTokensGet_DefaultsOffsetAndLimit()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    Offset = 0,
                    Limit = 50,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1);

        await TokensEndpoints.HandleTokensGet(httpContext);

        await sender
            .Received(1)
            .GetTokensAsync(Arg.Any<OcpiRequestContext>(), null, null, 0, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTokensGet_ResponseContainsDataArray()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    Offset = 0,
                    Limit = 50,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1);

        await TokensEndpoints.HandleTokensGet(httpContext);

        var body = ReadResponseBody(httpContext);
        body.Should().Contain("\"status_code\":1000");
        body.Should().Contain("\"data\":[]");
    }

    [Fact]
    public async Task HandleTokensGet_ParsesDateParameters()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    Offset = 0,
                    Limit = 50,
                }
            );

        var httpContext = CreateTokensGetContext(
            sender,
            OcpiVersion.V2_2_1,
            "?date_from=2024-01-01T00:00:00Z&date_to=2024-02-01T00:00:00Z"
        );

        await TokensEndpoints.HandleTokensGet(httpContext);

        await sender
            .Received(1)
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<DateTimeOffset?>(d => d!.Value.Year == 2024 && d.Value.Month == 1),
                Arg.Is<DateTimeOffset?>(d => d!.Value.Year == 2024 && d.Value.Month == 2),
                0,
                50,
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTokensGet_NegativeOffset_ClampedToZero()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    Offset = 0,
                    Limit = 50,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1, "?offset=-5");

        await TokensEndpoints.HandleTokensGet(httpContext);

        await sender
            .Received(1)
            .GetTokensAsync(Arg.Any<OcpiRequestContext>(), null, null, 0, 50, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTokensGet_LinkHeaderPreservesDateFilters()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = new object[] { "token1" },
                    TotalCount = 5,
                    Offset = 0,
                    Limit = 1,
                }
            );

        var httpContext = CreateTokensGetContext(
            sender,
            OcpiVersion.V2_2_1,
            "?offset=0&limit=1&date_from=2024-01-01T00:00:00Z"
        );

        await TokensEndpoints.HandleTokensGet(httpContext);

        var link = httpContext.Response.Headers["Link"].ToString();
        link.Should().Contain("offset=1");
        link.Should().Contain("date_from=2024-01-01");
    }

    [Fact]
    public async Task HandleTokensGet_LimitClamped_ToMaximum()
    {
        var sender = Substitute.For<ITokensSender>();
        sender
            .GetTokensAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<DateTimeOffset?>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(
                new PaginatedResult<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    Offset = 0,
                    Limit = 1000,
                }
            );

        var httpContext = CreateTokensGetContext(sender, OcpiVersion.V2_2_1, "?limit=9999");

        await TokensEndpoints.HandleTokensGet(httpContext);

        await sender
            .Received(1)
            .GetTokensAsync(Arg.Any<OcpiRequestContext>(), null, null, 0, 1000, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTokenAuthorize_CallsAuthorizer()
    {
        var authorizer = Substitute.For<ITokensAuthorizer>();
        authorizer
            .AuthorizeAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult<object>.Success(new { allowed = "ALLOWED" }));

        var httpContext = CreateAuthorizeContext(authorizer, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tokenUid"] = "TOKEN123";

        await TokensEndpoints.HandleTokenAuthorize(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("ALLOWED");

        await authorizer
            .Received(1)
            .AuthorizeAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is("TOKEN123"),
                Arg.Is<object?>(o => o == null),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTokenAuthorize_WithBody_DeserializesLocationReferences()
    {
        var authorizer = Substitute.For<ITokensAuthorizer>();
        authorizer
            .AuthorizeAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult<object>.Success(new { allowed = "ALLOWED" }));

        var httpContext = CreateAuthorizeContext(authorizer, OcpiVersion.V2_2_1, """{"location_id": "LOC1"}""");
        httpContext.Request.RouteValues["tokenUid"] = "TOKEN123";

        await TokensEndpoints.HandleTokenAuthorize(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await authorizer
            .Received(1)
            .AuthorizeAsync(
                Arg.Any<OcpiRequestContext>(),
                "TOKEN123",
                Arg.Is<object?>(o => o != null),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTokenAuthorize_InvalidJson_Returns400()
    {
        var authorizer = Substitute.For<ITokensAuthorizer>();
        var httpContext = CreateAuthorizeContext(authorizer, OcpiVersion.V2_2_1, "not valid json{{{");
        httpContext.Request.RouteValues["tokenUid"] = "TOKEN123";

        await TokensEndpoints.HandleTokenAuthorize(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("invalid JSON");
    }

    [Fact]
    public async Task HandleTokenAuthorize_AuthorizerFailure_ReturnsErrorStatus()
    {
        var authorizer = Substitute.For<ITokensAuthorizer>();
        authorizer
            .AuthorizeAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object?>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.GenericClientError, "Token unknown"));

        var httpContext = CreateAuthorizeContext(authorizer, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tokenUid"] = "UNKNOWN";

        await TokensEndpoints.HandleTokenAuthorize(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
        body.Should().Contain("Token unknown");
    }
}
