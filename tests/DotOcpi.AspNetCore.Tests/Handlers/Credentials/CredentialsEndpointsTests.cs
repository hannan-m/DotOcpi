using System.Text;
using DotOcpi.AspNetCore.Handlers.Credentials;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Handlers.Credentials;

public class CredentialsEndpointsTests
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
            ModuleId = "credentials",
        };

    private static DefaultHttpContext CreateHttpContext(
        ICredentialsHandler handler,
        OcpiVersion version,
        string? body = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(handler).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

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
    public async Task HandleCredentialsPost_V221_CallsHandler()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(new { token = "xyz" }));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, CredentialsJsonV221);

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

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
            .Returns(OcpiResult<object>.Success(new { token = "xyz" }));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_0, CredentialsJsonV20);

        await CredentialsEndpoints.HandleCredentialsPost(httpContext);

        await handler
            .Received(1)
            .OnCredentialsPostAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<object>(o => o is Models.V2_0.Credentials),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCredentialsPut_CallsHandler()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsPutAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(new { token = "xyz" }));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1, CredentialsJsonV221);

        await CredentialsEndpoints.HandleCredentialsPut(httpContext);

        await handler
            .Received(1)
            .OnCredentialsPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Is<object>(o => o is Models.V2_2_1.Credentials),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleCredentialsDelete_CallsHandler()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .OnCredentialsDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1);

        await CredentialsEndpoints.HandleCredentialsDelete(httpContext);

        await handler.Received(1).OnCredentialsDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleCredentialsGet_CallsHandler()
    {
        var handler = Substitute.For<ICredentialsHandler>();
        handler
            .GetCredentialsAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(new { token = "current" }));

        var httpContext = CreateHttpContext(handler, OcpiVersion.V2_2_1);

        await CredentialsEndpoints.HandleCredentialsGet(httpContext);

        await handler.Received(1).GetCredentialsAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<CancellationToken>());
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
}
