using System.Net;
using DotOcpi.Exceptions;
using DotOcpi.Registration;
using DotOcpi.Tests.Fixtures;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Registration;

public class VersionDiscoveryTests
{
    [Fact]
    public async Task GetVersionsAsync_ValidResponse_ReturnsVersions()
    {
        const string json = """
            {
                "status_code": 1000,
                "data": [
                    { "version": "2.2.1", "url": "https://cpo.example.com/ocpi/2.2.1" },
                    { "version": "2.1.1", "url": "https://cpo.example.com/ocpi/2.1.1" }
                ],
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        var versions = await discovery.GetVersionsAsync("https://cpo.example.com/ocpi/versions", "token-a");

        versions.Should().HaveCount(2);
        versions[0].Version.Should().Be("2.2.1");
        versions[0].Url.Should().Be("https://cpo.example.com/ocpi/2.2.1");
        versions[1].Version.Should().Be("2.1.1");
    }

    [Fact]
    public async Task GetVersionsAsync_SendsAuthorizationHeader()
    {
        const string json = """
            {
                "status_code": 1000,
                "data": [{ "version": "2.2.1", "url": "https://cpo.example.com/ocpi/2.2.1" }],
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        await discovery.GetVersionsAsync("https://cpo.example.com/ocpi/versions", "secret-token");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Token");
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("secret-token");
    }

    [Fact]
    public async Task GetVersionsAsync_NullData_Throws()
    {
        const string json = """
            {
                "status_code": 3000,
                "status_message": "Internal error",
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        var act = () => discovery.GetVersionsAsync("https://cpo.example.com/ocpi/versions", "token-a");

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*no data*");
    }

    [Fact]
    public async Task GetVersionsAsync_HttpError_Throws()
    {
        using var handler = new FakeHttpHandler(statusCode: HttpStatusCode.InternalServerError);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        var act = () => discovery.GetVersionsAsync("https://cpo.example.com/ocpi/versions", "token-a");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetVersionDetailAsync_ValidResponse_ReturnsDetail()
    {
        const string json = """
            {
                "status_code": 1000,
                "data": {
                    "version": "2.2.1",
                    "endpoints": [
                        { "identifier": "credentials", "role": "SENDER", "url": "https://cpo.example.com/ocpi/2.2.1/credentials" },
                        { "identifier": "locations", "role": "SENDER", "url": "https://cpo.example.com/ocpi/2.2.1/locations" }
                    ]
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        var detail = await discovery.GetVersionDetailAsync("https://cpo.example.com/ocpi/2.2.1", "token-a");

        detail.Version.Should().Be("2.2.1");
        detail.Endpoints.Should().HaveCount(2);
        detail.Endpoints[0].Identifier.Should().Be("credentials");
        detail.Endpoints[0].Role.Should().Be("SENDER");
        detail.Endpoints[1].Identifier.Should().Be("locations");
    }

    [Fact]
    public async Task GetVersionDetailAsync_NullData_Throws()
    {
        const string json = """
            {
                "status_code": 3000,
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(json);
        using var httpClient = new HttpClient(handler);
        var discovery = new VersionDiscovery(httpClient);

        var act = () => discovery.GetVersionDetailAsync("https://cpo.example.com/ocpi/2.2.1", "token-a");

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*no data*");
    }
}
