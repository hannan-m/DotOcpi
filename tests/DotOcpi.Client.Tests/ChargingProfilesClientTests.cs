using DotOcpi.Client.Tests.Internal;
using DotOcpi.Registry;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class ChargingProfilesClientTests
{
    private static CpoConnection CreateConnection(OcpiVersion version = OcpiVersion.V2_2_1) =>
        PullClientTestHelper.CreateConnection(
            version,
            new Dictionary<string, string>
            {
                ["locations"] = "https://cpo.example.com/ocpi/2.2.1/cpo/locations",
                ["chargingprofiles"] = "https://cpo.example.com/ocpi/2.2.1/cpo/chargingprofiles",
            }
        );

    private static string ProfileResponseJson =>
        """{"status_code": 1000, "data": {"result": "ACCEPTED", "timeout": 30}, "timestamp": "2024-01-01T00:00:00Z"}""";

    [Fact]
    public async Task SetChargingProfileAsync_V2_2_1_ReturnsSuccess()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(ProfileResponseJson);

        var connection = CreateConnection();
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.SetChargingProfileAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Put);
        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("chargingprofiles/SESSION1");
    }

    [Fact]
    public async Task SetChargingProfileAsync_V2_0_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        var connection = CreateConnection(OcpiVersion.V2_0);
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.SetChargingProfileAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeFalse();
        result.StatusMessage.Should().Contain("not supported");
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChargingProfileAsync_V2_1_1_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        var connection = CreateConnection(OcpiVersion.V2_1_1);
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.SetChargingProfileAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeFalse();
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteChargingProfileAsync_SendsDeleteRequest()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(ProfileResponseJson);

        var connection = CreateConnection();
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.DeleteChargingProfileAsync("DE:ALL", "SESSION1");

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task GetActiveChargingProfileAsync_SendsGetRequest()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(ProfileResponseJson);

        var connection = CreateConnection();
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.GetActiveChargingProfileAsync("DE:ALL", "SESSION1");

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Get);
    }

    [Fact]
    public async Task DeleteChargingProfileAsync_V2_0_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        var connection = CreateConnection(OcpiVersion.V2_0);
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.DeleteChargingProfileAsync("DE:ALL", "SESSION1");

        result.IsSuccess.Should().BeFalse();
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task SetChargingProfileAsync_SetsAuthHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(ProfileResponseJson);

        var connection = CreateConnection();
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await client.SetChargingProfileAsync("DE:ALL", "SESSION1", new object());

        var request = handler.SentRequests[0];
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Contains("X-Request-ID").Should().BeTrue();
    }

    [Fact]
    public async Task SetChargingProfileAsync_ErrorResponse_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            """{"status_code": 3000, "status_message": "CPO error"}""",
            System.Net.HttpStatusCode.InternalServerError
        );

        var connection = CreateConnection();
        var client = new ChargingProfilesClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.SetChargingProfileAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeFalse();
    }
}
