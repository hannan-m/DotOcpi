using System.Text;
using System.Text.Json;
using DotOcpi.AspNetCore.Handlers.Tariffs;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Handlers.Tariffs;

public class TariffsEndpointsTests
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
            ModuleId = "tariffs",
        };

    private static DefaultHttpContext CreateHttpContext(
        ITariffsReceiver receiver,
        OcpiVersion version,
        string? body = null
    )
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.RequestServices = new ServiceCollection().AddSingleton(receiver).BuildServiceProvider();
        httpContext.SetOcpiContext(CreateContext(version));

        if (body is not null)
        {
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";
        }

        return httpContext;
    }

    private const string TariffJsonV221 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "TAR1",
            "currency": "EUR",
            "elements": [{"price_components": [{"type": "TIME", "price": 2.0, "vat": 0.19, "step_size": 300}]}],
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    [Fact]
    public async Task HandleTariffPut_CallsReceiver()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .OnTariffPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, TariffJsonV221);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffPut(httpContext);

        await receiver
            .Received(1)
            .OnTariffPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "TAR1",
                Arg.Is<object>(o => o is Models.V2_2_1.Tariff),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTariffPatchRejected_Returns405()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetOcpiContext(CreateContext(OcpiVersion.V2_2_1));

        await TariffsEndpoints.HandleTariffPatchRejected(httpContext);

        httpContext.Response.StatusCode.Should().Be(405);
    }

    [Fact]
    public async Task HandleTariffPatch_V20_CallsReceiver()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .OnTariffPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_0, """{"currency": "USD"}""");
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffPatch(httpContext);

        await receiver
            .Received(1)
            .OnTariffPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "TAR1",
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTariffDelete_CallsReceiver()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .OnTariffDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffDelete(httpContext);

        await receiver
            .Received(1)
            .OnTariffDeleteAsync(Arg.Any<OcpiRequestContext>(), "TAR1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTariffGet_CallsReceiver()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .GetTariffAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Not found"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffGet(httpContext);

        await receiver.Received(1).GetTariffAsync(Arg.Any<OcpiRequestContext>(), "TAR1", Arg.Any<CancellationToken>());
    }
}
