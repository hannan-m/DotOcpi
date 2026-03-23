using System.Text.Json;
using DotOcpi.AspNetCore.Handlers.Credentials;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Credentials;

public class CredentialsEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        ICredentialsHandler handler,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(handler, version, "credentials", body);

    private const string CredentialsJsonV221 = """
        {
            "token": "abc123",
            "url": "https://cpo.example.com/ocpi/versions",
            "roles": [{"role": "CPO", "business_details": {"name": "Example CPO"}, "party_id": "ALL", "country_code": "DE"}]
        }
        """;

    private const string CredentialsJsonV20 = """
        {
            "token": "abc123",
            "url": "https://cpo.example.com/ocpi/versions",
            "business_name": "Example CPO",
            "party_id": "ALL",
            "country_code": "DE"
        }
        """;

    [Fact]
    public async Task HandleCredentialsPost_V221_DeserializesAndReturnsData()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(JsonDocument.Parse("""{"token": "xyz"}""").RootElement));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, CredentialsJsonV221);

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("xyz");

        await handler
            .Received(1)
            .OnCredentialsPostAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<object>(o => o is Models.V2_2_1.Credentials),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCredentialsPost_V20_UsesCorrectModel()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(JsonDocument.Parse("""{"token": "xyz"}""").RootElement));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_0, CredentialsJsonV20);

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await handler
            .Received(1)
            .OnCredentialsPostAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<object>(o => o is Models.V2_0.Credentials),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCredentialsPut_ReturnsResponseWithData()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPutAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(JsonDocument.Parse("""{"token": "new-token"}""").RootElement));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, CredentialsJsonV221);

        await CredentialsEndpoints.HandleCredentialsPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("new-token");

        await handler
            .Received(1)
            .OnCredentialsPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<object>(o => o is Models.V2_2_1.Credentials),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCredentialsDelete_ReturnsSuccess()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1);

        await CredentialsEndpoints.HandleCredentialsDelete(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await handler.Received(1).OnCredentialsDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCredentialsGet_ReturnsDataFromHandler()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .GetCredentialsAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(
                OcpiResult<object>.Success(
                    JsonDocument.Parse("""{"token": "current-token", "url": "https://example.com"}""").RootElement
                )
            );

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1);

        await CredentialsEndpoints.HandleCredentialsGet(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("current-token");
    }

    [Fact]
    public async Task HandleCredentialsPost_EmptyBody_Returns400()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleCredentialsPost_HandlerFailure_Returns400()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.GenericClientError, "Invalid credentials"));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, CredentialsJsonV221);

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
        body.Should().Contain("Invalid credentials");
    }

    [Fact]
    public async Task HandleCredentialsPut_InvalidJson_Returns400()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, "not valid json");

        await CredentialsEndpoints.HandleCredentialsPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }
}
