using System.Text;
using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using NSubstitute;
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

    private static OcpiHttpRequestBuilder CreateBuilder(CpoConnection? connection = null)
    {
        var registry = Substitute.For<ICpoRegistry>();
        var conn = connection ?? TestConnection;
        registry.FindByConnectionKey(conn.ConnectionKey).Returns(conn);
        return new OcpiHttpRequestBuilder(registry);
    }

    [Fact]
    public void Build_SetsAuthorizationHeader()
    {
        var builder = CreateBuilder();
        var request = builder.Build(HttpMethod.Get, "DE:ALL", "locations", "LOC1", "my-cpo-token");

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Token");
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        decoded.Should().Be("my-cpo-token");
    }

    [Fact]
    public void Build_SetsRequestIdAndCorrelationId()
    {
        var builder = CreateBuilder();
        var request = builder.Build(HttpMethod.Get, "DE:ALL", "locations", null, "token");

        request.Headers.Contains("X-Request-ID").Should().BeTrue();
        request.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        request.Headers.GetValues("X-Request-ID").Single().Should().NotBeEmpty();
        request.Headers.GetValues("X-Correlation-ID").Single().Should().NotBeEmpty();
    }

    [Fact]
    public void Build_GeneratesUniqueRequestIds()
    {
        var builder = CreateBuilder();
        var request1 = builder.Build(HttpMethod.Get, "DE:ALL", "locations", null, "token");
        var request2 = builder.Build(HttpMethod.Get, "DE:ALL", "locations", null, "token");

        var id1 = request1.Headers.GetValues("X-Request-ID").Single();
        var id2 = request2.Headers.GetValues("X-Request-ID").Single();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void Build_ConstructsUrlFromModuleEndpoint()
    {
        var builder = CreateBuilder();
        var request = builder.Build(HttpMethod.Get, "DE:ALL", "locations", "DE/ALL/LOC1", "token");

        request.RequestUri!.ToString().Should().Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations/DE/ALL/LOC1");
    }

    [Fact]
    public void Build_WithoutPathSuffix_UsesModuleUrlDirectly()
    {
        var builder = CreateBuilder();
        var request = builder.Build(HttpMethod.Get, "DE:ALL", "locations", null, "token");

        request.RequestUri!.ToString().Should().Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations");
    }

    [Fact]
    public void Build_WithBody_SerializesAsJson()
    {
        var builder = CreateBuilder();
        var body = new { location_id = "LOC1", evse_uid = "EVSE1" };
        var request = builder.Build(HttpMethod.Post, "DE:ALL", "commands", "START_SESSION", "token", body);

        request.Content.Should().NotBeNull();
        request.Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    [Fact]
    public void Build_WithoutBody_HasNoContent()
    {
        var builder = CreateBuilder();
        var request = builder.Build(HttpMethod.Get, "DE:ALL", "locations", "LOC1", "token");

        request.Content.Should().BeNull();
    }

    [Fact]
    public void Build_SetsCorrectHttpMethod()
    {
        var builder = CreateBuilder();

        builder.Build(HttpMethod.Get, "DE:ALL", "locations", null, "t").Method.Should().Be(HttpMethod.Get);
        builder.Build(HttpMethod.Put, "DE:ALL", "locations", "L1", "t").Method.Should().Be(HttpMethod.Put);
        builder.Build(HttpMethod.Post, "DE:ALL", "commands", "c", "t").Method.Should().Be(HttpMethod.Post);
        builder.Build(HttpMethod.Delete, "DE:ALL", "locations", "L1", "t").Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public void Build_CpoNotFound_ThrowsInvalidOperationException()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("XX:YY").Returns((CpoConnection?)null);
        var builder = new OcpiHttpRequestBuilder(registry);

        var act = () => builder.Build(HttpMethod.Get, "XX:YY", "locations", null, "token");

        act.Should().Throw<InvalidOperationException>().WithMessage("*XX:YY*not found*");
    }

    [Fact]
    public void Build_ModuleNotAvailable_ThrowsInvalidOperationException()
    {
        var builder = CreateBuilder();

        var act = () => builder.Build(HttpMethod.Get, "DE:ALL", "chargingprofiles", null, "token");

        act.Should().Throw<InvalidOperationException>().WithMessage("*chargingprofiles*not available*");
    }

    [Fact]
    public void BuildWithQuery_AppendsQueryString()
    {
        var builder = CreateBuilder();
        var request = builder.BuildWithQuery(
            HttpMethod.Get,
            "DE:ALL",
            "locations",
            "date_from=2024-01-01T00:00:00Z&offset=0&limit=50",
            "token"
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

    [Fact]
    public void GetConnection_ReturnsConnection()
    {
        var builder = CreateBuilder();
        var connection = builder.GetConnection("DE:ALL");

        connection.CpoCountryCode.Should().Be("DE");
        connection.Version.Should().Be(OcpiVersion.V2_2_1);
    }

    [Fact]
    public void GetConnection_NotFound_ThrowsInvalidOperationException()
    {
        var registry = Substitute.For<ICpoRegistry>();
        registry.FindByConnectionKey("XX:YY").Returns((CpoConnection?)null);
        var builder = new OcpiHttpRequestBuilder(registry);

        var act = () => builder.GetConnection("XX:YY");

        act.Should().Throw<InvalidOperationException>();
    }
}
