using System.Text;
using DotOcpi.AspNetCore.Handlers.Commands;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Handlers.Commands;

public class CommandsEndpointsTests
{
    private static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>(),
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static OcpiRequestContext CreateContext(OcpiVersion version) =>
        new()
        {
            Connection = TestConnection with { Version = version },
            RequestId = "req-1",
            CorrelationId = "corr-1",
            CpoId = "DE_ALL",
            CpoIdentity = new PartyIdentity("DE", "ALL"),
            EmspIdentity = new PartyIdentity("NL", "TNM"),
            NegotiatedVersion = version,
            ModuleId = "commands",
        };

    private static DefaultHttpContext CreateHttpContext(
        ICommandsCallback callback,
        OcpiVersion version,
        string? body = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(callback).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    [Fact]
    public async Task HandleCommandCallback_V221_CallsCallback()
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
        httpContext.Request.RouteValues["correlationId"] = "cmd-123";

        await CommandsEndpoints.HandleCommandCallback(httpContext);

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
        httpContext.Request.RouteValues["correlationId"] = "cmd-456";

        await CommandsEndpoints.HandleCommandCallback(httpContext);

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
        httpContext.Request.RouteValues["correlationId"] = "cmd-789";

        await CommandsEndpoints.HandleCommandCallback(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }
}
