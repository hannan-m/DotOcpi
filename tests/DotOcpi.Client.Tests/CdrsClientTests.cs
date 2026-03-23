using DotOcpi.Client.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class CdrsClientTests
{
    [Fact]
    public async Task GetAllCdrsAsync_SinglePage_ReturnsAllItems()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapList(TestJsonData.Cdr, TestJsonData.Cdr2));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new CdrsClient(new HttpClient(handler), PullClientTestHelper.CreateContextProvider(connection));

        var items = new List<object>();
        await foreach (var item in client.GetAllCdrsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCdrsAsync_MultiplePages_FollowsLinkHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Cdr),
            headers: new Dictionary<string, string>
            {
                ["X-Total-Count"] = "2",
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/cdrs?offset=1&limit=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Cdr2),
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var connection = PullClientTestHelper.CreateConnection();
        var client = new CdrsClient(new HttpClient(handler), PullClientTestHelper.CreateContextProvider(connection));

        var items = new List<object>();
        await foreach (var item in client.GetAllCdrsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        handler.SentRequests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllCdrsAsync_WithDateRange_IncludesQueryParams()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new CdrsClient(new HttpClient(handler), PullClientTestHelper.CreateContextProvider(connection));

        await foreach (
            var _ in client.GetAllCdrsAsync(
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
    public async Task GetAllCdrsAsync_ErrorResponse_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapError(), System.Net.HttpStatusCode.InternalServerError);

        var connection = PullClientTestHelper.CreateConnection();
        var client = new CdrsClient(new HttpClient(handler), PullClientTestHelper.CreateContextProvider(connection));

        var items = new List<object>();
        await foreach (var item in client.GetAllCdrsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllCdrsAsync_NoDates_NoQueryString()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new CdrsClient(new HttpClient(handler), PullClientTestHelper.CreateContextProvider(connection));

        await foreach (var _ in client.GetAllCdrsAsync("DE:ALL")) { }

        handler.SentRequests[0].RequestUri!.Query.Should().BeEmpty();
    }
}
