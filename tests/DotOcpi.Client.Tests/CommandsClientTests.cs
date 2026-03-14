using DotOcpi.Client.Tests.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class CommandsClientTests
{
    private static CpoConnection CreateConnection() =>
        PullClientTestHelper.CreateConnection(
            endpoints: new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
                ["sessions"] = "https://cpo.example.com/ocpi/2.2.1/cpo/sessions",
                ["cdrs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/cdrs",
                ["tariffs"] = "https://cpo.example.com/ocpi/2.2.1/cpo/tariffs",
                ["tokens"] = "https://cpo.example.com/ocpi/2.2.1/emsp/tokens",
                ["commands"] = "https://cpo.example.com/ocpi/2.2.1/cpo/commands",
            }
        );

    private static string CommandResponseJson =>
        """{"status_code": 1000, "data": {"result": "ACCEPTED", "timeout": 30}, "timestamp": "2024-01-01T00:00:00Z"}""";

    [Fact]
    public async Task SendStartSessionAsync_Success_ReturnsCommandResponse()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var command = new Models.V2_2_1.StartSession
        {
            Token = new Models.V2_2_1.Token
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
            },
            LocationId = "LOC1",
            ResponseUrl = "https://emsp.example.com/ocpi/commands/START_SESSION/abc123",
        };
        var result = await client.SendStartSessionAsync("DE:ALL", command);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeOfType<Models.V2_2_1.CommandResponse>();
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Post);
        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("commands/START_SESSION");
    }

    [Fact]
    public async Task SendStopSessionAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var command = new Models.V2_2_1.StopSession
        {
            SessionId = "SESSION1",
            ResponseUrl = "https://emsp.example.com/ocpi/commands/STOP_SESSION/abc123",
        };
        await client.SendStopSessionAsync("DE:ALL", command);

        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("commands/STOP_SESSION");
    }

    [Fact]
    public async Task SendReserveNowAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.SendReserveNowAsync("DE:ALL", new { response_url = "https://emsp.example.com/cb" });

        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("commands/RESERVE_NOW");
    }

    [Fact]
    public async Task SendUnlockConnectorAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.SendUnlockConnectorAsync("DE:ALL", new { response_url = "https://emsp.example.com/cb" });

        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("commands/UNLOCK_CONNECTOR");
    }

    [Fact]
    public async Task SendCancelReservationAsync_PostsToCorrectUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.SendCancelReservationAsync("DE:ALL", new { response_url = "https://emsp.example.com/cb" });

        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("commands/CANCEL_RESERVATION");
    }

    [Fact]
    public async Task SendCommand_ErrorResponse_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 2001, "status_message": "Invalid command"}""");

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.SendStartSessionAsync("DE:ALL", new object());

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2001);
    }

    [Fact]
    public async Task SendCommand_SetsAuthAndRequestHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.SendStartSessionAsync("DE:ALL", new object());

        var request = handler.SentRequests[0];
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Contains("X-Request-ID").Should().BeTrue();
    }

    [Fact]
    public async Task SendCommand_SerializesBody()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(CommandResponseJson);

        var connection = CreateConnection();
        var client = new CommandsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var command = new Models.V2_2_1.StopSession
        {
            SessionId = "SESSION1",
            ResponseUrl = "https://emsp.example.com/cb",
        };
        await client.SendStopSessionAsync("DE:ALL", command);

        var body = await handler.SentRequests[0].Content!.ReadAsStringAsync();
        body.Should().Contain("SESSION1");
    }
}
