using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotOcpi.Integration.Tests.Fixtures;
using DotOcpi.Testing;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Integration.Tests;

public class ModuleDataFlowTests : IAsyncLifetime
{
    private OcpiTestCpoServer _server = null!;

    public async Task InitializeAsync()
    {
        _server = await OcpiTestCpoServer.CreateAsync(c =>
        {
            c.Locations =
            [
                new
                {
                    id = "LOC1",
                    name = "Test Location 1",
                    address = "123 Test St",
                    city = "Berlin",
                    country = "DEU",
                },
                new
                {
                    id = "LOC2",
                    name = "Test Location 2",
                    address = "456 Other St",
                    city = "Munich",
                    country = "DEU",
                },
            ];
            c.Tariffs =
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
                                    price = 0.25m,
                                    step_size = 1,
                                },
                            },
                        },
                    },
                },
            ];
            c.Sessions =
            [
                new
                {
                    id = "SES1",
                    status = "ACTIVE",
                    kwh = 15.5,
                },
            ];
            c.Cdrs =
            [
                new
                {
                    id = "CDR1",
                    total_cost = 12.50m,
                    total_energy = 25.0,
                },
            ];
        });
    }

    public async Task DisposeAsync()
    {
        await _server.DisposeAsync();
    }

    [Fact]
    public async Task GetLocations_ReturnsAllConfiguredLocations()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(2);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("LOC1");
        body.GetProperty("data")[1].GetProperty("id").GetString().Should().Be("LOC2");
    }

    [Fact]
    public async Task GetLocations_SetsPaginationHeaders()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/locations");

        response.Headers.GetValues("X-Total-Count").Should().ContainSingle().Which.Should().Be("2");
        response.Headers.GetValues("X-Limit").Should().ContainSingle().Which.Should().Be("1000");
    }

    [Fact]
    public async Task GetTariffs_ReturnsTariffData()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/tariffs");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("TAR1");
        body.GetProperty("data")[0].GetProperty("currency").GetString().Should().Be("EUR");
    }

    [Fact]
    public async Task GetSessions_ReturnsSessionData()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/sessions");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("SES1");
    }

    [Fact]
    public async Task GetCdrs_ReturnsCdrData()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };
        var response = await client.GetAsync("/ocpi/cdrs");

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("data").GetArrayLength().Should().Be(1);
        body.GetProperty("data")[0].GetProperty("id").GetString().Should().Be("CDR1");
    }

    [Fact]
    public async Task AllResponses_IncludeTimestamp()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };

        var locations = await client.GetAsync("/ocpi/locations");
        var locBody = await locations.Content.ReadFromJsonAsync<JsonElement>();
        locBody.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();

        var tariffs = await client.GetAsync("/ocpi/tariffs");
        var tarBody = await tariffs.Content.ReadFromJsonAsync<JsonElement>();
        tarBody.GetProperty("timestamp").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AllResponses_HaveCorrectContentType()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };

        var response = await client.GetAsync("/ocpi/versions");
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task VersionEndpoints_ContainAbsoluteUrls()
    {
        using var client = new HttpClient { BaseAddress = _server.BaseUrl };

        var versionsResponse = await client.GetAsync("/ocpi/versions");
        var versionsBody = await versionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        var versionUrl = versionsBody.GetProperty("data")[0].GetProperty("url").GetString();
        versionUrl.Should().StartWith("http://");

        var detailResponse = await client.GetAsync(new Uri(versionUrl!));
        var detailBody = await detailResponse.Content.ReadFromJsonAsync<JsonElement>();
        var credentialsUrl = detailBody
            .GetProperty("data")
            .GetProperty("endpoints")
            .EnumerateArray()
            .First(e => e.GetProperty("identifier").GetString() == "credentials")
            .GetProperty("url")
            .GetString();
        credentialsUrl.Should().StartWith("http://");
    }
}
