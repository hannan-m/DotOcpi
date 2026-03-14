using System.Text.Json;
using DotOcpi.AspNetCore.Handlers.Tariffs;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Tariffs;

public class TariffsEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        ITariffsReceiver receiver,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(receiver, version, "tariffs", body);

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
    public async Task HandleTariffPut_CallsReceiverAndReturnsSuccess()
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

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

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
    public async Task HandleTariffPut_ReceiverFailure_Returns400()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .OnTariffPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericClientError, "Invalid tariff"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, TariffJsonV221);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
    }

    [Fact]
    public async Task HandleTariffPatchRejected_Returns405()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetOcpiContext(CreateContext(OcpiVersion.V2_2_1, "tariffs"));

        await TariffsEndpoints.HandleTariffPatchRejected(httpContext);

        httpContext.Response.StatusCode.Should().Be(405);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
        body.Should().Contain("PATCH is not supported");
    }

    [Fact]
    public async Task HandleTariffPatch_V20_CallsReceiverAndReturnsSuccess()
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

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await receiver
            .Received(1)
            .OnTariffPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "TAR1",
                Arg.Is<JsonElement>(e => e.GetProperty("currency").GetString() == "USD"),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleTariffDelete_CallsReceiverAndReturnsSuccess()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .OnTariffDeleteAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffDelete(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await receiver
            .Received(1)
            .OnTariffDeleteAsync(Arg.Any<OcpiRequestContext>(), "TAR1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleTariffGet_ReturnsDataFromReceiver()
    {
        var tariffData = new { id = "TAR1", currency = "EUR" };
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .GetTariffAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(tariffData));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffGet(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("TAR1");
    }

    [Fact]
    public async Task HandleTariffGet_NotFound_Returns400()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        receiver
            .GetTariffAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Not found"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffGet(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2003");
    }

    [Fact]
    public async Task HandleTariffPut_EmptyBody_Returns400()
    {
        var receiver = Substitute.For<ITariffsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());
        httpContext.Request.RouteValues["tariffId"] = "TAR1";

        await TariffsEndpoints.HandleTariffPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }
}
