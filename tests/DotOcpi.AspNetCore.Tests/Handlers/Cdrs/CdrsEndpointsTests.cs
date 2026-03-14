using System.Text;
using DotOcpi.AspNetCore.Handlers.Cdrs;
using DotOcpi.Modules;
using DotOcpi.Registry;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Xunit;

namespace DotOcpi.AspNetCore.Tests.Handlers.Cdrs;

public class CdrsEndpointsTests
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
            ModuleId = "cdrs",
        };

    private static DefaultHttpContext CreateHttpContext(
        ICdrsReceiver receiver,
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

    private static string ReadResponseBody(DefaultHttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        return reader.ReadToEnd();
    }

    // Minimal valid CDR JSON — all required fields for V2_2_1 deserialization
    private const string CdrJsonV221 = """
        {
            "country_code": "DE",
            "party_id": "ALL",
            "id": "CDR1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "end_date_time": "2024-01-01T12:00:00Z",
            "cdr_token": {"country_code": "NL", "party_id": "TNM", "uid": "T1", "type": "RFID", "contract_id": "C1"},
            "auth_method": "AUTH_REQUEST",
            "cdr_location": {
                "id": "LOC1",
                "address": "St 1",
                "city": "Berlin",
                "country": "DEU",
                "coordinates": {"latitude": "52.5", "longitude": "13.4"},
                "evse_uid": "EVSE1",
                "evse_id": "DE*ALL*EEVSE1",
                "connector_id": "1",
                "connector_standard": "IEC_62196_T2",
                "connector_format": "SOCKET",
                "connector_power_type": "AC_3_PHASE"
            },
            "currency": "EUR",
            "total_cost": {"excl_vat": 10.0},
            "total_energy": 30.0,
            "total_time": 2.0,
            "charging_periods": [],
            "last_updated": "2024-01-01T12:00:00Z"
        }
        """;

    // V2_0 CDR — no country_code/party_id, uses embedded Location, TotalCost is decimal
    private const string CdrJsonV20 = """
        {
            "id": "CDR1",
            "start_date_time": "2024-01-01T10:00:00Z",
            "end_date_time": "2024-01-01T12:00:00Z",
            "auth_id": "TOKEN1",
            "auth_method": "AUTH_REQUEST",
            "location": {
                "id": "LOC1",
                "address": "St 1",
                "city": "Berlin",
                "postal_code": "10115",
                "country": "DEU",
                "coordinates": {"latitude": "52.5", "longitude": "13.4"}
            },
            "currency": "EUR",
            "total_cost": 10.0,
            "total_energy": 30.0,
            "total_time": 2.0,
            "charging_periods": []
        }
        """;

    [Fact]
    public async Task HandleCdrPost_NewCdr_Returns201WithLocationHeader()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .OnCdrPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<CdrPostResult>.Success(new CdrPostResult("CDR1", true)));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, CdrJsonV221);
        httpContext.Request.Path = "/ocpi/2.2.1/cdrs";

        await CdrsEndpoints.HandleCdrPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(201);
        httpContext.Response.Headers["Location"].ToString().Should().Contain("CDR1");
    }

    [Fact]
    public async Task HandleCdrPost_DuplicateCdr_Returns200()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .OnCdrPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<CdrPostResult>.Success(new CdrPostResult("CDR1", false)));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, CdrJsonV221);
        httpContext.Request.Path = "/ocpi/2.2.1/cdrs";

        await CdrsEndpoints.HandleCdrPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task HandleCdrPost_V221_LocationHeaderIncludesPartyId()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .OnCdrPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<CdrPostResult>.Success(new CdrPostResult("CDR1", true)));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, CdrJsonV221);
        httpContext.Request.Path = "/ocpi/2.2.1/cdrs";

        await CdrsEndpoints.HandleCdrPost(httpContext);

        var location = httpContext.Response.Headers["Location"].ToString();
        location.Should().Contain("DE/ALL/CDR1");
    }

    [Fact]
    public async Task HandleCdrPost_ReceiverFailure_Returns400()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .OnCdrPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<CdrPostResult>.Failure(OcpiStatusCode.GenericClientError, "Invalid CDR data"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1, CdrJsonV221);

        await CdrsEndpoints.HandleCdrPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleCdrPost_EmptyBody_Returns400()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.Body = new MemoryStream(Array.Empty<byte>());

        await CdrsEndpoints.HandleCdrPost(httpContext);

        httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task HandleCdrPost_V20_LocationHeaderOmitsPartyPrefix()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .OnCdrPostAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<CdrPostResult>.Success(new CdrPostResult("CDR1", true)));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_0, CdrJsonV20);
        httpContext.Request.Path = "/ocpi/2.0/cdrs";

        await CdrsEndpoints.HandleCdrPost(httpContext);

        var location = httpContext.Response.Headers["Location"].ToString();
        location.Should().Be("/ocpi/2.0/cdrs/CDR1");
        location.Should().NotContain("DE/ALL");
    }

    [Fact]
    public async Task HandleCdrGet_CallsReceiverWithCorrectId()
    {
        var receiver = Substitute.For<ICdrsReceiver>();
        receiver
            .GetCdrAsync(Arg.Any<OcpiRequestContext>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(OcpiResult<object>.Failure(OcpiStatusCode.UnknownLocation, "CDR not found"));

        var httpContext = CreateHttpContext(receiver, OcpiVersion.V2_2_1);
        httpContext.Request.RouteValues["cdrId"] = "CDR1";

        await CdrsEndpoints.HandleCdrGet(httpContext);

        await receiver.Received(1).GetCdrAsync(Arg.Any<OcpiRequestContext>(), "CDR1", Arg.Any<CancellationToken>());
    }
}
