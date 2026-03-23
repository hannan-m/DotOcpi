using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Simulator;
using DotOcpi.Simulator.Models;
using DotOcpi.Simulator.State;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Simulator.Tests;

public class TypedModeTests : IAsyncLifetime
{
    private OcpiCpoSimulator _server = null!;

    public async Task InitializeAsync()
    {
        _server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.LocationSpecs =
            [
                new LocationSpec
                {
                    Id = "LOC1",
                    Name = "Berlin Fast Charger",
                    Evses =
                    [
                        new EvseSpec
                        {
                            Uid = "EVSE001",
                            EvseId = "DE*CPO*E001",
                            Connector = ConnectorProfile.DcFast,
                        },
                    ],
                },
            ];
            c.RequireAuth = false;
        });
    }

    public async Task DisposeAsync() => await _server.DisposeAsync();

    [Fact]
    public async Task GetLocations_TypedMode_ReturnsProperOcpiModel()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(1000);

        var data = body.GetProperty("data");
        data.GetArrayLength().Should().Be(1);

        var location = data[0];
        location.GetProperty("country_code").GetString().Should().Be("DE");
        location.GetProperty("party_id").GetString().Should().Be("CPO");
        location.GetProperty("id").GetString().Should().Be("LOC1");
        location.GetProperty("name").GetString().Should().Be("Berlin Fast Charger");
        location.GetProperty("publish").GetBoolean().Should().BeTrue();
        location.TryGetProperty("evses", out var evses).Should().BeTrue();
        evses.GetArrayLength().Should().Be(1);

        var evse = evses[0];
        evse.GetProperty("uid").GetString().Should().Be("EVSE001");
        evse.GetProperty("status").GetString().Should().Be("AVAILABLE");
    }

    [Fact]
    public async Task GetSingleLocation_TypedMode_ReturnsById()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations/LOC1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task GetEvse_TypedMode_ReturnsSubResource()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations/LOC1/EVSE001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("uid").GetString().Should().Be("EVSE001");
    }

    [Fact]
    public async Task GetConnector_TypedMode_ReturnsSubResource()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations/LOC1/EVSE001/1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("1");
        body.GetProperty("data").GetProperty("standard").GetString().Should().Be("IEC_62196_T2_COMBO");
    }

    [Fact]
    public async Task StartSession_TypedMode_CreatesSessionAndChangesEvseStatus()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var command = new { response_url = "https://example.com/cb", token = new { uid = "T1", contract_id = "NL-MSP-001" }, location_id = "LOC1", evse_uid = "EVSE001" };
        var response = await client.PostAsJsonAsync("/ocpi/commands/START_SESSION", command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify session created
        var sessions = _server.GetActiveSessions();
        sessions.Should().HaveCount(1);
        sessions[0].LocationId.Should().Be("LOC1");
        sessions[0].EvseUid.Should().Be("EVSE001");
        sessions[0].Phase.Should().Be(SessionPhase.Active);

        // Verify EVSE status changed to Charging
        var locResponse = await client.GetAsync("/ocpi/locations/LOC1");
        var locBody = await locResponse.Content.ReadFromJsonAsync<JsonElement>();
        var evse = locBody.GetProperty("data").GetProperty("evses")[0];
        evse.GetProperty("status").GetString().Should().Be("CHARGING");
    }

    [Fact]
    public async Task StopSession_TypedMode_CompletesSessionAndGeneratesCdr()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };

        // Start session
        var startCmd = new { response_url = "https://example.com/cb", token = new { uid = "T1", contract_id = "NL-MSP-001" }, location_id = "LOC1", evse_uid = "EVSE001" };
        await client.PostAsJsonAsync("/ocpi/commands/START_SESSION", startCmd);

        var sessions = _server.GetActiveSessions();
        var sessionId = sessions[0].SessionId;

        // Stop session
        var stopCmd = new { response_url = "https://example.com/cb", session_id = sessionId };
        await client.PostAsJsonAsync("/ocpi/commands/STOP_SESSION", stopCmd);

        // Session should be completed
        var session = _server.GetSession(sessionId);
        session!.Phase.Should().Be(SessionPhase.Completed);
        session.EndTime.Should().NotBeNull();

        // Verify session found and phase changed
        var stoppedSession = _server.GetSession(sessionId);
        stoppedSession.Should().NotBeNull("session should still exist after stop");
        stoppedSession!.Phase.Should().Be(SessionPhase.Completed);

        // CDR should be generated
        _server.Config.Cdrs.Should().NotBeEmpty("CDR should have been added to config.Cdrs");
        var cdrResponse = await client.GetAsync("/ocpi/cdrs");
        var cdrBody = await cdrResponse.Content.ReadFromJsonAsync<JsonElement>();
        cdrBody.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);

        // EVSE should be back to Available
        var locResponse = await client.GetAsync("/ocpi/locations/LOC1");
        var locBody = await locResponse.Content.ReadFromJsonAsync<JsonElement>();
        locBody.GetProperty("data").GetProperty("evses")[0].GetProperty("status").GetString().Should().Be("AVAILABLE");
    }

    [Fact]
    public async Task OcpiHeaders_PresentOnAllResponses()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/versions");

        response.Headers.Contains("X-Request-ID").Should().BeTrue();
        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        response.Headers.GetValues("Cache-Control").Should().ContainSingle("no-store");
        response.Headers.GetValues("X-Frame-Options").Should().ContainSingle("DENY");
    }
}
