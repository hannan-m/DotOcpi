using DotOcpi.Client.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class SessionsClientTests
{
    [Fact]
    public async Task GetAllSessionsAsync_SinglePage_ReturnsAllItems()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapList(TestJsonData.Session, TestJsonData.Session2));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllSessionsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllSessionsAsync_WithDateFrom_IncludesQueryParam()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await foreach (
            var _ in client.GetAllSessionsAsync(
                "DE:ALL",
                dateFrom: new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)
            )
        ) { }

        handler.SentRequests[0].RequestUri!.Query.Should().Contain("date_from=");
    }

    [Fact]
    public async Task PutChargingPreferencesAsync_V2_2_1_SendsRequest()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "data": "ACCEPTED", "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = PullClientTestHelper.CreateConnection(OcpiVersion.V2_2_1);
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var prefs = new Models.V2_2_1.ChargingPreferences { ProfileType = Models.V2_2_1.ProfileType.FAST };
        var result = await client.PutChargingPreferencesAsync("DE:ALL", "SESSION1", prefs);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(1);
        handler.SentRequests[0].Method.Should().Be(HttpMethod.Put);
        handler.SentRequests[0].RequestUri!.ToString().Should().Contain("sessions/SESSION1/charging_preferences");
    }

    [Fact]
    public async Task PutChargingPreferencesAsync_V2_0_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        var connection = PullClientTestHelper.CreateConnection(OcpiVersion.V2_0);
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.PutChargingPreferencesAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeFalse();
        result.StatusMessage.Should().Contain("not supported");
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task PutChargingPreferencesAsync_V2_1_1_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        var connection = PullClientTestHelper.CreateConnection(OcpiVersion.V2_1_1);
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var result = await client.PutChargingPreferencesAsync("DE:ALL", "SESSION1", new object());

        result.IsSuccess.Should().BeFalse();
        handler.SentRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task PutChargingPreferencesAsync_V2_2_SendsRequest()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "data": "ACCEPTED", "timestamp": "2024-01-01T00:00:00Z"}""");

        var connection = PullClientTestHelper.CreateConnection(OcpiVersion.V2_2);
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var prefs = new Models.V2_2.ChargingPreferences { ProfileType = Models.V2_2.ProfileType.FAST };
        var result = await client.PutChargingPreferencesAsync("DE:ALL", "SESSION1", prefs);

        result.IsSuccess.Should().BeTrue();
        handler.SentRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllSessionsAsync_ErrorResponse_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapError(), System.Net.HttpStatusCode.InternalServerError);

        var connection = PullClientTestHelper.CreateConnection();
        var client = new SessionsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllSessionsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }
}
