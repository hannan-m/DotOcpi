using DotOcpi.AspNetCore.Handlers.Commands;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Commands;

public class CommandsEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        ICommandsCallback callback,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(callback, version, "commands", body);

    [Fact]
    public async Task HandleCommandCallback_V221_DeserializesCommandResult()
    {
        var callback = Substitute.For<ICommandsCallback>();
        callback
            .OnCommandResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, """{"result": "ACCEPTED"}""");
        await CommandsEndpoints.HandleCommandCallback("cmd-123", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await callback
            .Received(1)
            .OnCommandResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is("cmd-123"),
                Arg.Is<object>(o => o is Models.V2_2_1.CommandResult),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCommandCallback_V20_UsesCommandResponse()
    {
        var callback = Substitute.For<ICommandsCallback>();
        callback
            .OnCommandResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_0, """{"result": "ACCEPTED", "timeout": 30}""");
        await CommandsEndpoints.HandleCommandCallback("cmd-456", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await callback
            .Received(1)
            .OnCommandResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is("cmd-456"),
                Arg.Is<object>(o => o is Models.V2_0.CommandResponse),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCommandCallback_EmptyBody_Returns400()
    {
        var callback = Substitute.For<ICommandsCallback>();
        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());
        await CommandsEndpoints.HandleCommandCallback("cmd-789", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleCommandCallback_InvalidJson_Returns400()
    {
        var callback = Substitute.For<ICommandsCallback>();
        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, "not json{{{");
        await CommandsEndpoints.HandleCommandCallback("cmd-000", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("invalid JSON");
    }

    [Fact]
    public async Task HandleCommandCallback_CallbackFailure_Returns400()
    {
        var callback = Substitute.For<ICommandsCallback>();
        callback
            .OnCommandResultAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericClientError, "Unknown correlation ID"));

        var httpContext = CreateHttpContext(callback, OcpiVersion.V2_2_1, """{"result": "ACCEPTED"}""");
        await CommandsEndpoints.HandleCommandCallback("unknown-id", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
    }
}
