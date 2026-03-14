using System.Text.Json;
using DotOcpi.AspNetCore.Handlers.Sessions;
using DotOcpi.Modules;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using static DotOcpi.AspNetCore.Tests.Handlers.OcpiEndpointTestHelper;

namespace DotOcpi.AspNetCore.Tests.Handlers.Sessions;

public class SessionsEndpointsTests
{
    private static DefaultHttpContext CreateHttpContext(
        ISessionsReceiver receiver,
        OcpiVersion version,
        string? body = null
    ) => OcpiEndpointTestHelper.CreateHttpContext(receiver, version, "sessions", body);

    private const string SessionJsonV221 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "SES1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "kwh": 15.5,
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "TOKEN1", "type": "RFID", "contract_id": "NLTNM123456"},
            "auth_method": "AUTH_REQUEST",
            "location_id": "LOC1",
            "evse_uid": "EVSE1",
            "connector_id": "1",
            "currency": "EUR",
            "status": "ACTIVE",
            "last_updated": "2024-01-01T10:30:00Z"
        }
        """;

    private const string SessionJsonV20 = """
        {
            "id": "SES1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "kwh": 15.5,
            "auth_id": "TOKEN1",
            "auth_method": "AUTH_REQUEST",
            "location": {"id": "LOC1", "address": "Str 1", "city": "Berlin", "postal_code": "10115", "country": "DEU", "coordinates": {"latitude": "52.5", "longitude": "13.4"}},
            "currency": "EUR",
            "status": "ACTIVE"
        }
        """;

    [Fact]
    public async Task HandleSessionPut_V221_DeserializesAndCallsReceiver()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .OnSessionPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, SessionJsonV221);
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");

        await receiver
            .Received(1)
            .OnSessionPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "SES1",
                Arg.Is<object>(o => o is Models.V2_2_1.Session),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleSessionPut_V20_DeserializesCorrectModelType()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .OnSessionPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_0, SessionJsonV20);
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnSessionPutAsync(
                Arg.Any<OcpiRequestContext>(),
                "SES1",
                Arg.Is<object>(o => o is Models.V2_0.Session),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleSessionPut_EmptyBody_Returns400()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleSessionPut_ReceiverFailure_Returns400()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .OnSessionPutAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<object>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Failure(OcpiStatusCode.GenericClientError, "Invalid session"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, SessionJsonV221);
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPut(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2000");
    }

    [Fact]
    public async Task HandleSessionPatch_PassesJsonElement()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .OnSessionPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                Arg.Any<string>(),
                Arg.Any<JsonElement>(),
                Arg.Any<CancellationToken>()
            )
            .Returns(OcpiResult.Success());

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, """{"kwh": 20.0}""");
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPatch(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        await receiver
            .Received(1)
            .OnSessionPatchAsync(
                Arg.Any<OcpiRequestContext>(),
                "SES1",
                Arg.Is<JsonElement>(e => e.GetProperty("kwh").GetDouble() == 20.0),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task HandleSessionPatch_ArrayBody_Returns400()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, """[1, 2, 3]""");
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionPatch(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleSessionGet_ReturnsData()
    {
        var sessionData = new Models.V2_2_1.Session
        {
            CountryCode = new CiString("DE"),
            PartyId = new CiString("ALL"),
            Id = new CiString("SES1"),
            StartDateTime = DateTimeOffset.Parse(
                "2024-01-01T10:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture
            ),
            Kwh = 15.5m,
            CdrToken = new Models.V2_2_1.CdrToken
            {
                CountryCode = new CiString("NL"),
                PartyId = new CiString("TNM"),
                Uid = new CiString("TOKEN1"),
                Type = Models.V2_2_1.TokenType.RFID,
                ContractId = new CiString("NLTNM123456"),
            },
            AuthMethod = Models.V2_2_1.AuthMethod.AUTH_REQUEST,
            LocationId = new CiString("LOC1"),
            EvseUid = new CiString("EVSE1"),
            ConnectorId = new CiString("1"),
            Currency = "EUR",
            Status = Models.V2_2_1.SessionStatus.ACTIVE,
            LastUpdated = DateTimeOffset.Parse(
                "2024-01-01T10:30:00Z",
                System.Globalization.CultureInfo.InvariantCulture
            ),
        };

        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .GetSessionAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Success(sessionData));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["sessionId"] = "SES1";

        await SessionsEndpoints.HandleSessionGet(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("1000");
        body.Should().Contain("SES1");
    }

    [Fact]
    public async Task HandleSessionGet_NotFound_Returns400()
    {
        var receiver = Substitute.For<ISessionsReceiver>();
        receiver
            .GetSessionAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "Session not found"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["sessionId"] = "UNKNOWN";

        await SessionsEndpoints.HandleSessionGet(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
        var body = ReadResponseBody(httpContext);
        body.Should().Contain("2003");
    }
}
