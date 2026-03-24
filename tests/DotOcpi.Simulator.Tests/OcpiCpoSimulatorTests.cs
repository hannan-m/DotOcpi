using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotOcpi.Simulator;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotOcpi.Simulator.Tests;

public class OcpiCpoSimulatorTests : IAsyncLifetime
{
    private OcpiCpoSimulator _server = null!;

    public async Task InitializeAsync()
    {
        _server = await OcpiCpoSimulator.CreateAsync();
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task CreateAsync_StartsServerOnRandomPort()
    {
        _server.BaseUrl.Should().NotBeNull();
        _server.BaseUrl.Port.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TokenA_IsNonEmpty()
    {
        _server.TokenA.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetIssuedTokenB_BeforeExchange_Throws()
    {
        var act = () => _server.GetIssuedTokenB();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetReceivedTokenC_BeforeExchange_ReturnsNull()
    {
        _server.GetReceivedTokenC().Should().BeNull();
    }

    [Fact]
    public async Task GetVersions_ReturnsConfiguredVersions()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(new Uri(_server.BaseUrl, "/ocpi/versions"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("version").GetString().Should().Be("2.2.1");
    }

    [Fact]
    public async Task GetVersions_WithMultipleVersions_ReturnsAll()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
            c.SupportedVersions = [OcpiVersion.V2_1_1, OcpiVersion.V2_2_1]
        );

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task GetVersionDetail_ValidVersion_ReturnsEndpoints()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(new Uri(_server.BaseUrl, "/ocpi/versions/2.2.1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("version").GetString().Should().Be("2.2.1");

        var endpoints = body.GetProperty("data").GetProperty("endpoints");
        endpoints.GetArrayLength().Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetVersionDetail_UnsupportedVersion_Returns404()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(new Uri(_server.BaseUrl, "/ocpi/versions/2.0"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
    }

    [Fact]
    public async Task PostCredentials_WithTokenA_ReturnsTokenB()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _server.TokenA);

        var credentialsPayload = new { token = "my-token-c", url = "https://example.com/ocpi/versions" };
        var response = await client.PostAsJsonAsync(new Uri(_server.BaseUrl, "/ocpi/credentials"), credentialsPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("data").GetProperty("roles").GetArrayLength().Should().Be(1);

        _server.GetIssuedTokenB().Should().NotBeNullOrWhiteSpace();
        _server.GetReceivedTokenC().Should().Be("my-token-c");
    }

    [Fact]
    public async Task PostCredentials_WithoutAuth_Returns401()
    {
        using var client = CreateClient();
        var response = await client.PostAsJsonAsync(new Uri(_server.BaseUrl, "/ocpi/credentials"), new { token = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostCredentials_WithWrongToken_Returns401()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "wrong-token");

        var response = await client.PostAsJsonAsync(new Uri(_server.BaseUrl, "/ocpi/credentials"), new { token = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutCredentials_WithTokenB_RotatesToken()
    {
        await PerformRegistration();
        var tokenB = _server.GetIssuedTokenB();

        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", tokenB);

        var response = await client.PutAsJsonAsync(
            new Uri(_server.BaseUrl, "/ocpi/credentials"),
            new { token = "rotated-c", url = "https://example.com/ocpi/versions" }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var newTokenB = _server.GetIssuedTokenB();
        newTokenB.Should().NotBe(tokenB);
        _server.GetReceivedTokenC().Should().Be("rotated-c");
    }

    [Fact]
    public async Task DeleteCredentials_WithTokenB_ClearsState()
    {
        await PerformRegistration();
        var tokenB = _server.GetIssuedTokenB();

        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", tokenB);

        var response = await client.DeleteAsync(new Uri(_server.BaseUrl, "/ocpi/credentials"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _server.GetReceivedTokenC().Should().BeNull();
    }

    [Fact]
    public async Task GetLocations_ReturnsConfiguredData()
    {
        var location = new { id = "LOC1", name = "Test Location" };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [location]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Total-Count").Should().ContainSingle().Which.Should().Be("1");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetTariffs_ReturnsConfiguredData()
    {
        var tariff = new { id = "TAR1", currency = "EUR" };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Tariffs = [tariff]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/tariffs"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetSessions_ReturnsEmptyByDefault()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(new Uri(_server.BaseUrl, "/ocpi/sessions"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetCdrs_ReturnsEmptyByDefault()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(new Uri(_server.BaseUrl, "/ocpi/cdrs"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ForceStatusCode_OverridesResponseStatus()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.ForceStatusCode = new OcpiStatusCode(3000));

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(3000);
        body.GetProperty("status_message").GetString().Should().Be("Forced error");
    }

    [Fact]
    public async Task RejectRegistration_ReturnsBadRequest()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.RejectRegistration = true);

        using var client = CreateClient(server);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", server.TokenA);

        var response = await client.PostAsJsonAsync(new Uri(server.BaseUrl, "/ocpi/credentials"), new { token = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(3001);
    }

    [Fact]
    public async Task SimulateTimeout_ReturnsServerError()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.SimulateTimeout = true);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ResponseDelay_SlowsResponse()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
            c.ResponseDelay = TimeSpan.FromMilliseconds(100)
        );

        using var client = CreateClient(server);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));
        sw.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sw.ElapsedMilliseconds.Should().BeGreaterOrEqualTo(50);
    }

    [Fact]
    public async Task CpoIdentity_IncludedInCredentialsResponse()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
            c.CpoIdentity = new PartyIdentity("NL", "TST")
        );

        using var client = CreateClient(server);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", server.TokenA);

        var response = await client.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/credentials"),
            new { token = "c", url = "https://example.com" }
        );

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var role = body.GetProperty("data").GetProperty("roles")[0];
        role.GetProperty("country_code").GetString().Should().Be("NL");
        role.GetProperty("party_id").GetString().Should().Be("TST");
    }

    [Fact]
    public async Task GetSingleLocation_ReturnsItem()
    {
        var loc = new { id = "LOC1", name = "Test" };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [loc]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations/LOC1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task GetSingleLocation_NotFound_Returns404()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [new { id = "LOC1" }]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations/UNKNOWN"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(2003);
    }

    [Fact]
    public async Task GetSingleLocation_PartyPrefixed_ReturnsItem()
    {
        var loc = new { id = "LOC1", name = "Test" };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [loc]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/DE/CPO/locations/LOC1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task GetLocations_WithOffsetAndLimit_ReturnsPage()
    {
        var locs = new object[] { new { id = "L1" }, new { id = "L2" }, new { id = "L3" } };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [.. locs]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations?offset=1&limit=1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("L2");

        response.Headers.GetValues("X-Total-Count").Should().ContainSingle().Which.Should().Be("3");
        response.Headers.GetValues("Link").Should().ContainSingle();
    }

    [Fact]
    public async Task GetLocations_LastPage_NoLinkHeader()
    {
        var locs = new object[] { new { id = "L1" }, new { id = "L2" } };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [.. locs]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations?offset=1&limit=1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);

        response.Headers.Contains("Link").Should().BeFalse();
    }

    [Fact]
    public async Task GetLocations_PartyPrefixed_ReturnsList()
    {
        var loc = new { id = "LOC1" };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [loc]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/DE/CPO/locations"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PutToken_StoresInReceivedTokens()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var token = new
        {
            uid = "TOKEN1",
            type = "RFID",
            auth_id = "NL-TST-001",
        };
        var response = await client.PutAsync(new Uri(server.BaseUrl, "/ocpi/tokens/TOKEN1"), JsonContent.Create(token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task PatchToken_MergesIntoExistingToken()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);

        // First push a token
        var token = new
        {
            uid = "TOK1",
            type = "RFID",
            valid = true,
        };
        await client.PutAsync(new Uri(server.BaseUrl, "/ocpi/tokens/TOK1"), JsonContent.Create(token));

        // Patch it
        var patch = new { valid = false };
        var patchResponse = await client.PatchAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/TOK1"),
            JsonContent.Create(patch)
        );
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the stored token was merged
        server.ReceivedTokens.Should().ContainKey("TOK1");
        var stored = server.ReceivedTokens["TOK1"];
        stored.GetProperty("uid").GetString().Should().Be("TOK1");
        stored.GetProperty("valid").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task PatchToken_NotFound_Returns404()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var response = await client.PatchAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/UNKNOWN"),
            JsonContent.Create(new { valid = false })
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchToken_ExistingToken_ReturnsSuccess()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);

        // Push token first, then patch
        await client.PutAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/TOKEN1"),
            JsonContent.Create(new { uid = "TOKEN1", type = "RFID" })
        );
        var response = await client.PatchAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/TOKEN1"),
            JsonContent.Create(new { type = "APP_USER" })
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Command_StartSession_ReturnsAccepted()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var command = new { response_url = "https://emsp.example.com/callback", token = new { uid = "T1" } };
        var response = await client.PostAsJsonAsync(new Uri(server.BaseUrl, "/ocpi/commands/START_SESSION"), command);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("result").GetString().Should().Be("ACCEPTED");
    }

    [Fact]
    public async Task Command_ConfigurableResponse_ReturnsConfiguredStatus()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.CommandResponseStatus = "REJECTED");

        using var client = CreateClient(server);
        var response = await client.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/commands/STOP_SESSION"),
            new { response_url = "https://example.com" }
        );

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("result").GetString().Should().Be("REJECTED");
    }

    [Fact]
    public async Task PutChargingProfile_StoresProfile()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var profile = new { charging_rate_unit = "W", min_charging_rate = 0 };
        var response = await client.PutAsync(
            new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"),
            JsonContent.Create(profile)
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task GetChargingProfile_AfterPut_ReturnsStoredProfile()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var profile = new { charging_rate_unit = "W" };
        await client.PutAsync(new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"), JsonContent.Create(profile));

        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("charging_rate_unit").GetString().Should().Be("W");
    }

    [Fact]
    public async Task GetChargingProfile_NotFound_Returns404()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/charging_profiles/UNKNOWN"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(2003);
    }

    [Fact]
    public async Task DeleteChargingProfile_RemovesStoredProfile()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        await client.PutAsync(
            new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"),
            JsonContent.Create(new { rate = "W" })
        );

        var deleteResponse = await client.DeleteAsync(new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/charging_profiles/SESSION1"));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReceivedRequests_RecordsAllRequests()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [new { id = "LOC1" }]);

        using var client = CreateClient(server);
        await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));
        await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations"));

        var requests = server.GetReceivedRequests();
        requests.Should().HaveCount(2);
        requests[0].Method.Should().Be("GET");
        requests[0].Path.Should().Be("/ocpi/versions");
        requests[1].Path.Should().Be("/ocpi/locations");
    }

    [Fact]
    public async Task GetReceivedRequests_CapturesPostBody()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", server.TokenA);
        await client.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/credentials"),
            new { token = "test-token", url = "https://example.com" }
        );

        var requests = server.GetReceivedRequests();
        requests.Should().ContainSingle();
        requests[0].Body.Should().NotBeNull();
        requests[0].Body!.Value.GetProperty("token").GetString().Should().Be("test-token");
    }

    [Fact]
    public async Task ClearRequestHistory_ClearsAllRecords()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));
        server.GetReceivedRequests().Should().HaveCount(1);

        server.ClearRequestHistory();
        server.GetReceivedRequests().Should().BeEmpty();
    }

    [Fact]
    public async Task CommandCallback_PostsToResponseUrl()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.CommandCallbackEnabled = true;
            c.CommandCallbackDelay = TimeSpan.FromMilliseconds(50);
            c.CommandResponseStatus = "ACCEPTED";
        });

        // Spin up a tiny listener to capture the callback
        JsonElement? receivedCallback = null;
        var tcs = new TaskCompletionSource<bool>();

        var callbackBuilder = WebApplication.CreateBuilder();
        callbackBuilder.WebHost.ConfigureKestrel(o => o.Listen(IPAddress.Loopback, 0));
        var callbackApp = callbackBuilder.Build();
        callbackApp.MapPost(
            "/callback/{correlationId}",
            async (HttpContext ctx) =>
            {
                receivedCallback = await JsonSerializer.DeserializeAsync<JsonElement>(ctx.Request.Body);
                tcs.TrySetResult(true);
                ctx.Response.StatusCode = 200;
            }
        );
        await callbackApp.StartAsync();
        var callbackUrl = callbackApp.Urls.First();

        try
        {
            using var client = CreateClient(server);
            var command = new { response_url = $"{callbackUrl}/callback/CORR-123", token = new { uid = "T1" } };
            var response = await client.PostAsJsonAsync(
                new Uri(server.BaseUrl, "/ocpi/commands/START_SESSION"),
                command
            );
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            // Wait for async callback — the simulator fires this on the same process,
            // so it should arrive quickly. 5s is generous for slow CI.
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
            completed.Should().Be(tcs.Task, because: "callback should arrive within timeout");

            receivedCallback.Should().NotBeNull();
            receivedCallback!.Value.GetProperty("result").GetString().Should().Be("ACCEPTED");
        }
        finally
        {
            await callbackApp.StopAsync();
            await callbackApp.DisposeAsync();
        }
    }

    [Fact]
    public async Task EndpointOverride_ForcesErrorOnlyForTargetedModule()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.Locations = [new { id = "LOC1" }];
            c.Tariffs = [new { id = "TAR1" }];
            c.EndpointOverrides["locations"] = new() { ForceStatusCode = new OcpiStatusCode(3000) };
        });

        using var client = CreateClient(server);

        var locResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations"));
        var locBody = await locResponse.Content.ReadFromJsonAsync<JsonElement>();
        locBody.GetProperty("status_code").GetInt32().Should().Be(3000);

        var tarResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/tariffs"));
        var tarBody = await tarResponse.Content.ReadFromJsonAsync<JsonElement>();
        tarBody.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task EndpointOverride_SimulateTimeoutOnSpecificEndpoint()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.EndpointOverrides["tariffs"] = new() { SimulateTimeout = true };
        });

        using var client = CreateClient(server);

        var versionsResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));
        versionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tariffsResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/tariffs"));
        tariffsResponse.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task VersionDetail_IncludesAllModules()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions/2.2.1"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var endpoints = body.GetProperty("data").GetProperty("endpoints");

        var moduleIds = new List<string>();
        foreach (var ep in endpoints.EnumerateArray())
            moduleIds.Add(ep.GetProperty("identifier").GetString()!);

        moduleIds.Should().Contain("tokens");
        moduleIds.Should().Contain("commands");
        moduleIds.Should().Contain("charging_profiles");
    }

    [Fact]
    public async Task GetLocations_AfterRegistration_RequiresAuth()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [new { id = "LOC1" }]);

        // Register first to issue Token B
        using var regClient = CreateClient(server);
        regClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", server.TokenA);
        await regClient.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/credentials"),
            new { token = "c", url = "https://example.com" }
        );

        // Now try without auth — should get 401
        using var noAuthClient = CreateClient(server);
        var response = await noAuthClient.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // With Token B — should succeed
        using var authClient = CreateClient(server);
        authClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Token",
            server.GetIssuedTokenB()
        );
        var okResponse = await authClient.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations"));
        okResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetLocations_OffsetBeyondCount_ReturnsEmpty()
    {
        var locs = new object[] { new { id = "L1" }, new { id = "L2" } };
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.Locations = [.. locs]);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/locations?offset=100&limit=10"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(0);
        response.Headers.Contains("Link").Should().BeFalse();
    }

    [Fact]
    public async Task PatchToken_NonObjectBody_Returns400()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync();

        using var client = CreateClient(server);
        await client.PutAsync(new Uri(server.BaseUrl, "/ocpi/tokens/TOK1"), JsonContent.Create(new { uid = "TOK1" }));

        // Send a JSON array instead of object
        var response = await client.PatchAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/TOK1"),
            new StringContent("[1,2,3]", Encoding.UTF8, "application/json")
        );

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CommandCallback_MissingResponseUrl_NoCallback()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.CommandCallbackEnabled = true;
            c.CommandCallbackDelay = TimeSpan.FromMilliseconds(50);
        });

        using var client = CreateClient(server);
        // Send command without response_url
        var response = await client.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/commands/START_SESSION"),
            new { token = new { uid = "T1" } }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // No response_url means no callback was enqueued — should complete immediately
        var pending = server.WaitForPendingCallbacksAsync();
        pending.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task StartSession_CreatesActiveSession()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.RequireAuth = false);

        using var client = CreateClient(server);
        var command = new
        {
            response_url = "https://example.com/cb",
            token = new { uid = "T1" },
            location_id = "LOC1",
            evse_uid = "E1",
        };
        await client.PostAsJsonAsync(new Uri(server.BaseUrl, "/ocpi/commands/START_SESSION"), command);

        // Verify session was created
        var sessResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/sessions"));
        var body = await sessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessions = body.GetProperty("data");
        sessions.GetArrayLength().Should().BeGreaterThan(0);

        var session = sessions[sessions.GetArrayLength() - 1];
        session.GetProperty("status").GetString().Should().Be("ACTIVE");
        session.GetProperty("location_id").GetString().Should().Be("LOC1");
    }

    [Fact]
    public async Task StopSession_CompletesSessionAndCreatesCdr()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.RequireAuth = false);

        using var client = CreateClient(server);

        // Start a session first
        var startCmd = new
        {
            response_url = "https://example.com/cb",
            token = new { uid = "T1" },
            location_id = "LOC1",
            evse_uid = "E1",
        };
        await client.PostAsJsonAsync(new Uri(server.BaseUrl, "/ocpi/commands/START_SESSION"), startCmd);

        // Get the session ID
        var sessResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/sessions"));
        var sessBody = await sessResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = sessBody
            .GetProperty("data")[sessBody.GetProperty("data").GetArrayLength() - 1]
            .GetProperty("id")
            .GetString();

        // Stop it
        var stopCmd = new { response_url = "https://example.com/cb", session_id = sessionId };
        await client.PostAsJsonAsync(new Uri(server.BaseUrl, "/ocpi/commands/STOP_SESSION"), stopCmd);

        // Verify session is COMPLETED
        var sessResponse2 = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/sessions"));
        var body2 = await sessResponse2.Content.ReadFromJsonAsync<JsonElement>();
        var lastSession = body2.GetProperty("data")[body2.GetProperty("data").GetArrayLength() - 1];
        lastSession.GetProperty("status").GetString().Should().Be("COMPLETED");

        // Verify CDR was created
        var cdrResponse = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/cdrs"));
        var cdrBody = await cdrResponse.Content.ReadFromJsonAsync<JsonElement>();
        cdrBody.GetProperty("data").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Authorize_ReturnsConfiguredResult()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
        {
            c.RequireAuth = false;
            c.AuthorizationResult = "BLOCKED";
        });

        using var client = CreateClient(server);
        var response = await client.PostAsJsonAsync(
            new Uri(server.BaseUrl, "/ocpi/tokens/TOKEN1/authorize"),
            new { location_references = (object?)null }
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetProperty("allowed").GetString().Should().Be("BLOCKED");
    }

    private HttpClient CreateClient(OcpiCpoSimulator? server = null)
    {
        var target = server ?? _server;
        return new HttpClient { BaseAddress = target.BaseUrl };
    }

    private async Task PerformRegistration()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", _server.TokenA);

        await client.PostAsJsonAsync(
            new Uri(_server.BaseUrl, "/ocpi/credentials"),
            new { token = "initial-c", url = "https://example.com/ocpi/versions" }
        );
    }
}
