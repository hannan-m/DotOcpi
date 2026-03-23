using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Integration.Tests.Fixtures;
using DotOcpi.Registration;
using DotOcpi.Security;
using DotOcpi.Simulator;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Integration.Tests;

/// <summary>
/// Verifies that every response from the test CPO server returns the correct
/// OCPI status_code in the JSON envelope, matching the OCPI spec for each scenario.
/// </summary>
public class OcpiStatusCodeTests : IntegrationTestBase
{
    protected override void ConfigureServer(CpoSimulatorConfiguration config)
    {
        config.SupportedVersions = [OcpiVersion.V2_2_1, OcpiVersion.V2_1_1];
        config.Locations =
        [
            new
            {
                id = "LOC1",
                name = "Status Code Test",
                address = "1 Test Ave",
                city = "Berlin",
                country = "DEU",
            },
        ];
        config.Tariffs =
        [
            new
            {
                id = "TAR1",
                currency = "EUR",
                elements = new[]
                {
                    new
                    {
                        price_components = new[]
                        {
                            new
                            {
                                type = "ENERGY",
                                price = 0.30m,
                                step_size = 1,
                            },
                        },
                    },
                },
            },
        ];
        config.Sessions =
        [
            new
            {
                id = "SES1",
                status = "ACTIVE",
                kwh = 10.0,
            },
        ];
        config.Cdrs =
        [
            new
            {
                id = "CDR1",
                total_cost = 5.00m,
                total_energy = 20.0,
            },
        ];
    }

    // --- Success (1000) ---

    [Fact]
    public async Task VersionDiscovery_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/versions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("status_message").GetString().Should().Be("Success");
        body.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task VersionDetail_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/versions/2.2.1");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetProperty("version").GetString().Should().Be("2.2.1");
    }

    [Fact]
    public async Task PostCredentials_Success_Returns1000()
    {
        using var client = CreateHttpClient(Server.TokenA);

        var response = await client.PostAsJsonAsync(
            "/ocpi/credentials",
            new
            {
                token = TokenGenerator.Generate(),
                url = "https://emsp.example.com/ocpi/versions",
                roles = new[]
                {
                    new
                    {
                        role = "EMSP",
                        business_details = new { name = "Test eMSP" },
                        party_id = "MSP",
                        country_code = "NL",
                    },
                },
            }
        );
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetLocations_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/locations");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetTariffs_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/tariffs");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task GetSessions_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/sessions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task GetCdrs_Success_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/cdrs");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    [Fact]
    public async Task DeleteCredentials_Success_Returns1000()
    {
        await PerformRegistration();
        var tokenB = Server.GetIssuedTokenB();
        using var client = CreateHttpClient(tokenB);

        var response = await client.DeleteAsync("/ocpi/credentials");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
    }

    // --- Invalid/Missing Auth (2002) ---

    [Fact]
    public async Task PostCredentials_MissingAuth_Returns401With2002()
    {
        using var client = CreateHttpClient();

        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task PostCredentials_WrongToken_Returns401With2002()
    {
        using var client = CreateHttpClient("completely-wrong-token");

        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task PostCredentials_BearerInsteadOfToken_Returns401With2002()
    {
        using var client = CreateHttpClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer some-jwt");

        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task PutCredentials_WrongToken_Returns401With2002()
    {
        await PerformRegistration();
        using var client = CreateHttpClient("invalid-token");

        var response = await client.PutAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task DeleteCredentials_WrongToken_Returns401With2002()
    {
        await PerformRegistration();
        using var client = CreateHttpClient("invalid-token");

        var response = await client.DeleteAsync("/ocpi/credentials");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    [Fact]
    public async Task PutCredentials_TokenAInsteadOfB_Returns401With2002()
    {
        await PerformRegistration();
        using var client = CreateHttpClient(Server.TokenA);

        var response = await client.PutAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    // --- Unsupported Version (2001) ---

    [Fact]
    public async Task VersionDetail_UnsupportedVersion_Returns404With2001()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/versions/2.0");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
    }

    [Fact]
    public async Task VersionDetail_GarbageVersion_Returns404With2001()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/versions/9.9.9");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        body.GetProperty("status_code").GetInt32().Should().Be(2001);
    }

    // --- Registration Rejected (3001) ---

    [Fact]
    public async Task PostCredentials_RejectedByServer_Returns400With3001()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c => c.RejectRegistration = true);

        using var client = new HttpClient { BaseAddress = server.BaseUrl };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", server.TokenA);

        var response = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x", url = "https://x.com" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.GetProperty("status_code").GetInt32().Should().Be(3001);
        body.GetProperty("status_message").GetString().Should().Contain("rejected");
    }

    // --- Forced Server Error (3000) ---

    [Fact]
    public async Task ForceStatusCode_3000_ReturnsServerErrorInEnvelope()
    {
        await using var server = await OcpiCpoSimulator.CreateAsync(c =>
            c.ForceStatusCode = OcpiStatusCode.GenericServerError
        );

        using var client = new HttpClient { BaseAddress = server.BaseUrl };

        var response = await client.GetAsync("/ocpi/versions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(3000);
        body.GetProperty("status_message").GetString().Should().Be("Forced error");
    }

    // --- Token Rotation: old token invalidated ---

    [Fact]
    [Trait("Category", "Security")]
    public async Task TokenRotation_OldTokenB_Returns401With2002()
    {
        await PerformRegistration();
        var firstTokenB = Server.GetIssuedTokenB();

        // Rotate
        using var client = CreateHttpClient(firstTokenB);
        await client.PutAsJsonAsync(
            "/ocpi/credentials",
            new
            {
                token = TokenGenerator.Generate(),
                url = "https://example.com",
                roles = new[]
                {
                    new
                    {
                        role = "EMSP",
                        business_details = new { name = "Test" },
                        party_id = "MSP",
                        country_code = "NL",
                    },
                },
            }
        );

        var newTokenB = Server.GetIssuedTokenB();
        newTokenB.Should().NotBe(firstTokenB);

        // Old token should get 401 + 2002
        using var client2 = CreateHttpClient(firstTokenB);
        var response = await client2.PutAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        body.GetProperty("status_code").GetInt32().Should().Be(2002);
    }

    // --- Envelope structure ---

    [Fact]
    public async Task AllSuccessResponses_HaveRequiredEnvelopeFields()
    {
        using var client = CreateHttpClient();

        string[] paths = ["/ocpi/versions", "/ocpi/locations", "/ocpi/tariffs", "/ocpi/sessions", "/ocpi/cdrs"];

        foreach (var path in paths)
        {
            var response = await client.GetAsync(path);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();

            body.TryGetProperty("status_code", out _).Should().BeTrue($"missing status_code on {path}");
            body.TryGetProperty("timestamp", out _).Should().BeTrue($"missing timestamp on {path}");
            body.TryGetProperty("data", out _).Should().BeTrue($"missing data on {path}");
        }
    }

    [Fact]
    public async Task AllErrorResponses_HaveRequiredEnvelopeFields()
    {
        using var client = CreateHttpClient();

        // Unsupported version
        var response = await client.GetAsync("/ocpi/versions/9.9.9");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.TryGetProperty("status_code", out _).Should().BeTrue();
        body.TryGetProperty("status_message", out _).Should().BeTrue();
        body.TryGetProperty("timestamp", out _).Should().BeTrue();

        // Missing auth
        var response2 = await client.PostAsJsonAsync("/ocpi/credentials", new { token = "x" });
        var body2 = await response2.Content.ReadFromJsonAsync<JsonElement>();

        body2.TryGetProperty("status_code", out _).Should().BeTrue();
        body2.TryGetProperty("status_message", out _).Should().BeTrue();
        body2.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Timestamp_IsValidRfc3339()
    {
        using var client = CreateHttpClient();
        var response = await client.GetAsync("/ocpi/versions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        var timestamp = body.GetProperty("timestamp").GetString()!;
        DateTimeOffset.TryParse(timestamp, out var parsed).Should().BeTrue();
        parsed.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    // --- Multi-version status code ---

    [Fact]
    public async Task VersionDetail_SupportedV211_Returns1000()
    {
        using var client = CreateHttpClient();

        var response = await client.GetAsync("/ocpi/versions/2.1.1");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.GetProperty("status_code").GetInt32().Should().Be(1000);
        body.GetProperty("data").GetProperty("version").GetString().Should().Be("2.1.1");
    }

    // --- Helpers ---

    private async Task PerformRegistration()
    {
        using var httpClient = CreateHttpClient();
        var credentialsClient = new CredentialsClient(httpClient);

        await credentialsClient.PostCredentialsAsync(
            $"{Server.BaseUrl}ocpi/credentials",
            Server.TokenA,
            OcpiVersion.V2_2_1,
            TestCredentialsHelper.V2_2_1(TokenGenerator.Generate())
        );
    }
}
