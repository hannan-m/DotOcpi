using DotOcpi.Client.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class LocationsClientTests
{
    [Fact]
    public async Task GetLocationAsync_Success_ReturnsLocation()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapObject(TestJsonData.Location));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        var result = await client.GetLocationAsync("DE:ALL", "LOC1");

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<Models.V2_2_1.Location>();
    }

    [Fact]
    public async Task GetLocationAsync_ErrorResponse_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapError(2003, "Unknown location"));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        var result = await client.GetLocationAsync("DE:ALL", "LOC1");

        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Value.Should().Be(2003);
    }

    [Fact]
    public async Task GetLocationAsync_SetsAuthAndRequestHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapObject(TestJsonData.Location));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        await client.GetLocationAsync("DE:ALL", "LOC1");

        handler.SentRequests.Should().HaveCount(1);
        var request = handler.SentRequests[0];
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Contains("X-Request-ID").Should().BeTrue();
        request.Headers.Contains("X-Correlation-ID").Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationAsync_ConstructsCorrectUrl()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapObject(TestJsonData.Location));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        await client.GetLocationAsync("DE:ALL", "LOC1");

        handler
            .SentRequests[0]
            .RequestUri!.ToString()
            .Should()
            .Be("https://cpo.example.com/ocpi/2.2.1/cpo/locations/LOC1");
    }

    [Fact]
    public async Task GetAllLocationsAsync_SinglePage_ReturnsAllItems()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Location, TestJsonData.Location2),
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllLocationsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllLocationsAsync_MultiplePages_FollowsLinkHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Location),
            headers: new Dictionary<string, string>
            {
                ["X-Total-Count"] = "2",
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=1&limit=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Location2),
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllLocationsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        handler.SentRequests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllLocationsAsync_WithDateFrom_IncludesQueryParam()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        await foreach (
            var _ in client.GetAllLocationsAsync(
                "DE:ALL",
                dateFrom: new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)
            )
        ) { }

        handler.SentRequests[0].RequestUri!.Query.Should().Contain("date_from=");
    }

    [Fact]
    public async Task GetAllLocationsAsync_WithDateRange_IncludesBothParams()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        await foreach (
            var _ in client.GetAllLocationsAsync(
                "DE:ALL",
                dateFrom: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                dateTo: new DateTimeOffset(2024, 7, 1, 0, 0, 0, TimeSpan.Zero)
            )
        ) { }

        var query = handler.SentRequests[0].RequestUri!.Query;
        query.Should().Contain("date_from=");
        query.Should().Contain("date_to=");
    }

    [Fact]
    public async Task GetAllLocationsAsync_NoDates_NoQueryString()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        await foreach (var _ in client.GetAllLocationsAsync("DE:ALL")) { }

        handler.SentRequests[0].RequestUri!.Query.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLocationAsync_HttpError_ReturnsFailure()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapError(), System.Net.HttpStatusCode.InternalServerError);

        var connection = PullClientTestHelper.CreateConnection();
        var client = new LocationsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateContextProvider(connection)
        );

        var result = await client.GetLocationAsync("DE:ALL", "LOC1");

        result.IsSuccess.Should().BeFalse();
    }
}
