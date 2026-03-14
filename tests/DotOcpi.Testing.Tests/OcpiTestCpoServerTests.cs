using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DotOcpi.Testing;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Testing.Tests;

public class OcpiTestCpoServerTests : IAsyncLifetime
{
    private OcpiTestCpoServer _server = null!;

    public async Task InitializeAsync()
    {
        _server = await OcpiTestCpoServer.CreateAsync();
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
        await using var server = await OcpiTestCpoServer.CreateAsync(c =>
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
        await using var server = await OcpiTestCpoServer.CreateAsync(c => c.Locations = [location]);

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
        await using var server = await OcpiTestCpoServer.CreateAsync(c => c.Tariffs = [tariff]);

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
        await using var server = await OcpiTestCpoServer.CreateAsync(c => c.ForceStatusCode = new OcpiStatusCode(3000));

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status_code").GetInt32().Should().Be(3000);
        body.GetProperty("status_message").GetString().Should().Be("Forced error");
    }

    [Fact]
    public async Task RejectRegistration_ReturnsBadRequest()
    {
        await using var server = await OcpiTestCpoServer.CreateAsync(c => c.RejectRegistration = true);

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
        await using var server = await OcpiTestCpoServer.CreateAsync(c => c.SimulateTimeout = true);

        using var client = CreateClient(server);
        var response = await client.GetAsync(new Uri(server.BaseUrl, "/ocpi/versions"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task ResponseDelay_SlowsResponse()
    {
        await using var server = await OcpiTestCpoServer.CreateAsync(c =>
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
        await using var server = await OcpiTestCpoServer.CreateAsync(c =>
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

    private HttpClient CreateClient(OcpiTestCpoServer? server = null)
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
