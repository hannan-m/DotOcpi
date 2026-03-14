using System.Text.Json;
using DotOcpi.Client.Tests.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class TokensClientTests
{
    private static CpoConnection CreateConnection()
    {
        return PullClientTestHelper.CreateConnection(
            endpoints: new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
                ["sessions"] = "https://cpo.example.com/ocpi/2.2.1/cpo/sessions",
                ["cdrs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/cdrs",
                ["tariffs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tariffs",
                ["tokens"] = "https://cpo.example.com/ocpi/2.2.1/emsp/tokens",
            }
        );
    }

    [Fact]
    public async Task PushTokenAsync_Success_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var token = new Models.V2_2_1.Token
        {
            CountryCode = new CiString("NL"),
            PartyId = new CiString("TNM"),
            Uid = new CiString("012345678"),
            Type = Models.V2_2_1.TokenType.RFID,
            ContractId = new CiString("NL-TNM-000001"),
            Issuer = "TheNewMotion",
            Valid = true,
            Whitelist = Models.V2_2_1.WhitelistType.ALWAYS,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        var result = await client.PushTokenAsync("DE:ALL", "012345678", token);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(1);
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Put);
        handler
            .SentRequests[0]
            .RequestUri!.ToString()
            .Should()
            .Be("https://cpo.example.com/ocpi/2.2.1/emsp/tokens/012345678");
    }

    [Fact]
    public async Task PushTokenAsync_ErrorResponse_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 2001, "status_message": "Invalid token format"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.PushTokenAsync("DE:ALL", "012345678", new object());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2001);
    }

    [Fact]
    public async Task PushTokenAsync_SetsAuthAndRequestHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.PushTokenAsync("DE:ALL", "012345678", new object());

        var request = handler.SentRequests[0];
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Contains("X-Request-ID").Should().BeTrue();
        request.Headers.Contains("X-Correlation-ID").Should().BeTrue();
    }

    [Fact]
    public async Task PushTokenAsync_SerializesBody()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var token = new Models.V2_2_1.Token
        {
            CountryCode = new CiString("NL"),
            PartyId = new CiString("TNM"),
            Uid = new CiString("012345678"),
            Type = Models.V2_2_1.TokenType.RFID,
            ContractId = new CiString("NL-TNM-000001"),
            Issuer = "TheNewMotion",
            Valid = true,
            Whitelist = Models.V2_2_1.WhitelistType.ALWAYS,
            LastUpdated = DateTimeOffset.UtcNow,
        };

        await client.PushTokenAsync("DE:ALL", "012345678", token);

        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("\"uid\"");
        body.Should().Contain("012345678");
    }

    [Fact]
    public async Task PatchTokenAsync_Success_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var patch = JsonDocument.Parse("""{"valid": false}""").RootElement;
        var result = await client.PatchTokenAsync("DE:ALL", "012345678", patch);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(1);
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Patch);
    }

    [Fact]
    public async Task PatchTokenAsync_SendsJsonBody()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var patch = JsonDocument.Parse("""{"valid": false}""").RootElement;
        await client.PatchTokenAsync("DE:ALL", "012345678", patch);

        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("\"valid\"");
        body.Should().Contain("false");
    }

    [Fact]
    public async Task PatchTokenAsync_ErrorResponse_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 2003, "status_message": "Unknown token"}""");

        var connection = CreateConnection();
        var client = new TokensClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var patch = JsonDocument.Parse("""{"valid": false}""").RootElement;
        var result = await client.PatchTokenAsync("DE:ALL", "012345678", patch);

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2003);
    }
}
