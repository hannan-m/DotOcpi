using System.Net;
using System.Text;
using DotOcpi.Exceptions;
using DotOcpi.Registration;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Tests.Registration;

public class CredentialsClientTests
{
    [Fact]
    public async Task PostCredentials_V2_2_1_ExtractsRolesResponse()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-b",
                    "url": "https://cpo.example.com/ocpi/versions",
                    "roles": [
                        {
                            "role": "CPO",
                            "business_details": { "name": "Example CPO" },
                            "party_id": "EXA",
                            "country_code": "DE"
                        }
                    ]
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var result = await client.PostCredentialsAsync(
            "https://cpo.example.com/ocpi/2.2.1/credentials",
            "token-a",
            OcpiVersion.V2_2_1,
            new { token = "our-token-b", url = "https://emsp.example.com/ocpi/versions" }
        );

        result.Token.Should().Be("cpo-token-b");
        result.VersionsUrl.Should().Be("https://cpo.example.com/ocpi/versions");
        result.CountryCode.Should().Be("DE");
        result.PartyId.Should().Be("EXA");
    }

    [Fact]
    public async Task PostCredentials_V2_1_1_ExtractsFlatResponse()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-b",
                    "url": "https://cpo.example.com/ocpi/versions",
                    "business_name": "Example CPO",
                    "party_id": "EXA",
                    "country_code": "DE"
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var result = await client.PostCredentialsAsync(
            "https://cpo.example.com/ocpi/2.1.1/credentials",
            "token-a",
            OcpiVersion.V2_1_1,
            new { token = "our-token-b", url = "https://emsp.example.com/ocpi/versions" }
        );

        result.Token.Should().Be("cpo-token-b");
        result.CountryCode.Should().Be("DE");
        result.PartyId.Should().Be("EXA");
    }

    [Fact]
    public async Task PostCredentials_V2_0_ExtractsFlatResponse()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-b",
                    "url": "https://cpo.example.com/ocpi/versions",
                    "business_name": "Old CPO",
                    "party_id": "OLD",
                    "country_code": "NL"
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var result = await client.PostCredentialsAsync(
            "https://cpo.example.com/ocpi/2.0/credentials",
            "token-a",
            OcpiVersion.V2_0,
            new { token = "our-token-b", url = "https://emsp.example.com/ocpi/versions" }
        );

        result.Token.Should().Be("cpo-token-b");
        result.CountryCode.Should().Be("NL");
        result.PartyId.Should().Be("OLD");
    }

    [Fact]
    public async Task PostCredentials_MissingData_Throws()
    {
        const string responseJson = """
            {
                "status_code": 3000,
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var act = () =>
            client.PostCredentialsAsync(
                "https://cpo.example.com/ocpi/2.2.1/credentials",
                "token-a",
                OcpiVersion.V2_2_1,
                new { }
            );

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*missing data*");
    }

    [Fact]
    public async Task PostCredentials_V2_2_1_MissingRoles_Throws()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-b",
                    "url": "https://cpo.example.com/ocpi/versions"
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var act = () =>
            client.PostCredentialsAsync(
                "https://cpo.example.com/ocpi/2.2.1/credentials",
                "token-a",
                OcpiVersion.V2_2_1,
                new { }
            );

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*missing roles*");
    }

    [Fact]
    public async Task PostCredentials_MissingToken_Throws()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "url": "https://cpo.example.com/ocpi/versions",
                    "business_name": "Test",
                    "party_id": "TST",
                    "country_code": "DE"
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var act = () =>
            client.PostCredentialsAsync(
                "https://cpo.example.com/ocpi/2.1.1/credentials",
                "token-a",
                OcpiVersion.V2_1_1,
                new { }
            );

        await act.Should().ThrowAsync<OcpiRegistrationException>().WithMessage("*missing required field 'token'*");
    }

    [Fact]
    public async Task PutCredentials_SendsAuthAndReturnsResponse()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-c",
                    "url": "https://cpo.example.com/ocpi/versions",
                    "roles": [
                        {
                            "role": "CPO",
                            "business_details": { "name": "Example CPO" },
                            "party_id": "EXA",
                            "country_code": "DE"
                        }
                    ]
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var result = await client.PutCredentialsAsync(
            "https://cpo.example.com/ocpi/2.2.1/credentials",
            "token-b",
            OcpiVersion.V2_2_1,
            new { }
        );

        result.Token.Should().Be("cpo-token-c");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("token-b");
    }

    [Fact]
    public async Task DeleteCredentials_SendsDeleteWithAuth()
    {
        using var handler = new FakeHttpHandler("{}");
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        await client.DeleteCredentialsAsync("https://cpo.example.com/ocpi/2.2.1/credentials", "token-b");

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Token");
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("token-b");
    }

    [Fact]
    public async Task DeleteCredentials_HttpError_Throws()
    {
        using var handler = new FakeHttpHandler(statusCode: HttpStatusCode.MethodNotAllowed);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        var act = () => client.DeleteCredentialsAsync("https://cpo.example.com/ocpi/2.2.1/credentials", "token-b");

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task PostCredentials_SendsAuthorizationHeader()
    {
        const string responseJson = """
            {
                "status_code": 1000,
                "data": {
                    "token": "cpo-token-b",
                    "url": "https://cpo.example.com/ocpi/versions",
                    "business_name": "Test",
                    "party_id": "TST",
                    "country_code": "DE"
                },
                "timestamp": "2026-01-01T00:00:00Z"
            }
            """;

        using var handler = new FakeHttpHandler(responseJson);
        using var httpClient = new HttpClient(handler);
        var client = new CredentialsClient(httpClient);

        await client.PostCredentialsAsync(
            "https://cpo.example.com/ocpi/2.1.1/credentials",
            "my-token-a",
            OcpiVersion.V2_1_1,
            new { }
        );

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Token");
        handler.LastRequest!.Headers.Authorization!.Parameter.Should().Be("my-token-a");
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly string _responseBody;
        private readonly HttpStatusCode _statusCode;

        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpHandler(string responseBody = "{}", HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            _responseBody = responseBody;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            LastRequest = request;
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json"),
            };
            return Task.FromResult(response);
        }
    }
}
