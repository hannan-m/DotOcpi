using System.Text;
using System.Text.Json;
using DotOcpi.AspNetCore.Handlers.Locations;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Locations;

public class LocationsEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        ILocationsReceiver receiver,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(receiver, version, "locations", body);

    private const string LocationJsonV221 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "LOC1",
            "publish": true,
            "address": "Hauptstr 1",
            "city": "Berlin",
            "country": "DEU",
            "coordinates": {"latitude": "52.520008", "longitude": "13.404954"},
            "time_zone": "Europe/Berlin",
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    private const string LocationJsonV20 = """
        {
            "id": "LOC1",
            "address": "Hauptstr 1",
            "city": "Berlin",
            "postal_code": "10115",
            "country": "DEU",
            "coordinates": {"latitude": "52.520008", "longitude": "13.404954"}
        }
        """;

    private const string EvseJson = """
        {
            "uid": "EVSE1",
            "status": "AVAILABLE",
            "connectors": [],
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    private const string ConnectorJson = """
        {
            "id": "1",
            "standard": "IEC_62196_T2",
            "format": "SOCKET",
            "power_type": "AC_3_PHASE",
            "max_voltage": 230,
            "max_amperage": 32,
            "last_updated": "2024-01-01T00:00:00Z"
        }
        """;

    private const string PatchJson = """{"name":"Updated Name"}""";

    [Fact]
    public async Task HandleLocationPut_V221_DeserializesAndCallsReceiver()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, LocationJsonV221);
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                Arg.Is<object>(o => o is Models.V2_2_1.Location),
                Arg.Any<CancellationToken>()
            );

        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
    }

    [Fact]
    public async Task HandleLocationPut_V20_DeserializesCorrectModelType()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_0, LocationJsonV20);
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                Arg.Is<object>(o => o is Models.V2_0.Location),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleLocationPut_EmptyBody_Returns400()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2001");
        await receiver
            .DidNotReceive()
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleLocationPut_InvalidJson_Returns400()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, "not json{{{");
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("invalid JSON");
    }

    [Fact]
    public async Task HandleLocationPut_ReceiverClientError_Returns400()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericClientError, "Bad data"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, LocationJsonV221);
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
        body.Should().Contain("Bad data");
    }

    [Fact]
    public async Task HandleLocationPut_ReceiverServerError_Returns500()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnLocationPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericServerError, "Internal failure"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, LocationJsonV221);
        await LocationsEndpoints.HandleLocationPut("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(500);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("3000");
        body.Should().Contain("Internal failure");
    }

    [Fact]
    public async Task HandleLocationPatch_PassesJsonElementToReceiver()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnLocationPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, PatchJson);
        await LocationsEndpoints.HandleLocationPatch("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnLocationPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                Arg.Is<JsonElement>(e => e.GetProperty("name").GetString() == "Updated Name"),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleLocationGet_ReturnsDataFromReceiver()
    {
        var locationData = new Models.V2_2_1.Location
        {
            CountryCode = new CiString("DE"),
            PartyId = new CiString("ALL"),
            Id = new CiString("LOC1"),
            Publish = true,
            Address = "Hauptstr 1",
            City = "Berlin",
            Country = "DEU",
            Coordinates = new GeoLocation("52.520008", "13.404954"),
            TimeZone = "Europe/Berlin",
            LastUpdated = DateTimeOffset.Parse(
                "2024-01-01T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture
            ),
        };

        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .GetLocationAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(locationData));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        await LocationsEndpoints.HandleLocationGet("LOC1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("LOC1");
        body.Should().Contain("Hauptstr 1");
    }

    [Fact]
    public async Task HandleLocationGet_ReceiverFailure_Returns400()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .GetLocationAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Location not found"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        await LocationsEndpoints.HandleLocationGet("UNKNOWN", httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2003");
    }

    [Fact]
    public async Task HandleEvsePut_CallsReceiverWithCorrectIds()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnEvsePutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, EvseJson);

        await LocationsEndpoints.HandleEvsePut("LOC1", "EVSE1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnEvsePutAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                "EVSE1",
                Arg.Is<object>(o => o is Models.V2_2_1.Evse),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleEvsePatch_PassesJsonElementToReceiver()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnEvsePatchAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, PatchJson);

        await LocationsEndpoints.HandleEvsePatch("LOC1", "EVSE1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnEvsePatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                "EVSE1",
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleConnectorPut_CallsReceiverWithCorrectIds()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnConnectorPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, ConnectorJson);

        await LocationsEndpoints.HandleConnectorPut("LOC1", "EVSE1", "1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnConnectorPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                "EVSE1",
                "1",
                Arg.Is<object>(o => o is Models.V2_2_1.Connector),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleConnectorPatch_PassesJsonElementToReceiver()
    {
        var receiver = Substitute.For<ILocationsReceiver>();
        receiver
            .OnConnectorPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, PatchJson);

        await LocationsEndpoints.HandleConnectorPatch("LOC1", "EVSE1", "1", httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnConnectorPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "LOC1",
                "EVSE1",
                "1",
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            );
    }
}
