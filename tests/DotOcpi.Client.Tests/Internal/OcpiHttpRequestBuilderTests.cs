using System.Text.Json;
using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class OcpiHttpRequestBuilderTests
{
    private static readonly CpoConnection TestConnection = new()
    {
        CpoCountryCode = "DE",
        CpoPartyId = "ALL",
        EmspCountryCode = "NL",
        EmspPartyId = "TNM",
        Version = OcpiVersion.V2_2_1,
        ModuleEndpoints = new Dictionary<string, string>
        {
            ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
            ["tokens"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tokens",
            ["commands"] = "https://cpo.example.com/ocpi/2.2.1/cpo/commands",
        },
        TokenBHash = "hash",
        Status = ConnectionStatus.Connected,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static CpoConnectionContext CreateContext(
        CpoConnection? connection = null,
        string token = "my-cpo-token"
    ) => new() { Connection = connection ?? TestConnection, RawToken = token };

    [Fact]
    public void Build_SetsAuthorizationHeader_WithRawToken()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", "LOC1");

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Token");
        // Raw token sent as-is — consistent with CredentialsClient and VersionDiscovery.
        // No base64 encoding; the token is already a string from the credentials handshake.
        request.Headers.Authorization.Parameter.Should().Be("my-cpo-token");
    }

    [Fact]
    public void Build_SetsRequestIdAndCorrelationId()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", null);

        request.Headers.Contains("X-Request-ID").Should().BeTrue();
        request.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        request.Headers.GetValues("X-Request-ID").Single().Should().NotBeEmpty();
        request.Headers.GetValues("X-Correlation-ID").Single().Should().NotBeEmpty();
    }

    [Fact]
    public void Build_GeneratesUniqueRequestIds()
    {
        var context = CreateContext();
        var request1 = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", null);
        var request2 = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", null);

        var id1 = request1.Headers.GetValues("X-Request-ID").Single();
        var id2 = request2.Headers.GetValues("X-Request-ID").Single();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Build_ConstructsUrlFromModuleEndpoint()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", "DE/ALL/LOC1");

        request.RequestUri!.ToString().Should().Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations/DE/ALL/LOC1");
    }

    [Fact]
    public void Build_WithoutPathSuffix_UsesModuleUrlDirectly()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", null);

        request.RequestUri!.ToString().Should().Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations");
    }

    [Fact]
    public void Build_WithBody_SerializesAsJson()
    {
        var context = CreateContext();
        var body = JsonDocument.Parse("""{"location_id": "LOC1", "evse_uid": "EVSE1"}""").RootElement;
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Post, context, "commands", "START_SESSION", body);

        request.Content.Should().NotBeNull();
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public void Build_WithoutBody_HasNoContent()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", "LOC1");

        request.Content.Should().BeNull();
    }

    [Fact]
    public void Build_SetsCorrectHttpMethod()
    {
        var context = CreateContext(token: "t");

        OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", null).Method.Should().Be(HttpMethod.Get);
        OcpiHttpRequestBuilder.Build(HttpMethod.Put, context, "locations", "L1").Method.Should().Be(HttpMethod.Put);
        OcpiHttpRequestBuilder.Build(HttpMethod.Post, context, "commands", "c").Method.Should().Be(HttpMethod.Post);
        OcpiHttpRequestBuilder
            .Build(HttpMethod.Delete, context, "locations", "L1")
            .Method.Should()
            .Be(HttpMethod.Delete);
    }

    [Fact]
    public void Build_ModuleNotAvailable_ThrowsInvalidOperationException()
    {
        var context = CreateContext();

        var act = () => OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "chargingprofiles", null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*chargingprofiles*not available*");
    }

    [Fact]
    public void BuildWithQuery_AppendsQueryString()
    {
        var context = CreateContext();
        var request = OcpiHttpRequestBuilder.BuildWithQuery(
            HttpMethod.Get,
            context,
            "locations",
            "date_from=2024-01-01T00:00:00Z&offset=0&limit=50"
        );

        request.RequestUri!.ToString().Should().Contain("?date_from=2024-01-01T00:00:00Z&offset=0&limit=50");
    }

    [Fact]
    public void BuildForUrl_CreatesRequestForAbsoluteUrl()
    {
        var url = "https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=50&limit=50";
        var request = OcpiHttpRequestBuilder.BuildForUrl(HttpMethod.Get, url, "my-token");

        request.RequestUri!.ToString().Should().Be(url);
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Contains("X-Request-ID").Should().BeTrue();
    }
}
