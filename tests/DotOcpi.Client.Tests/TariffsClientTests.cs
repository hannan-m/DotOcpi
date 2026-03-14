using DotOcpi.Client.Tests.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests;

public class TariffsClientTests
{
    [Fact]
    public async Task GetAllTariffsAsync_SinglePage_ReturnsAllItems()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapList(TestJsonData.Tariff, TestJsonData.Tariff2));

        var connection = PullClientTestHelper.CreateConnection();
        var client = new TariffsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllTariffsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTariffsAsync_MultiplePages_FollowsLinkHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Tariff),
            headers: new Dictionary<string, string>
            {
                ["X-Total-Count"] = "2",
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/tariffs?offset=1&limit=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse(
            TestJsonData.WrapList(TestJsonData.Tariff2),
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var connection = PullClientTestHelper.CreateConnection();
        var client = new TariffsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllTariffsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        handler.SentRequests.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllTariffsAsync_WithDateFrom_IncludesQueryParam()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new TariffsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await foreach (
            var _ in client.GetAllTariffsAsync(
                "DE:ALL",
                dateFrom: new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero)
            )
        ) { }

        handler.SentRequests[0].RequestUri!.Query.Should().Contain("date_from=");
    }

    [Fact]
    public async Task GetAllTariffsAsync_NoDates_NoQueryString()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapEmpty());

        var connection = PullClientTestHelper.CreateConnection();
        var client = new TariffsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        await foreach (var _ in client.GetAllTariffsAsync("DE:ALL")) { }

        handler.SentRequests[0].RequestUri!.Query.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTariffsAsync_ErrorResponse_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(TestJsonData.WrapError(), System.Net.HttpStatusCode.InternalServerError);

        var connection = PullClientTestHelper.CreateConnection();
        var client = new TariffsClient(
            new HttpClient(handler),
            PullClientTestHelper.CreateBuilder(connection),
            PullClientTestHelper.CreateTokenProvider()
        );

        var items = new List<object>();
        await foreach (var item in client.GetAllTariffsAsync("DE:ALL"))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }
}
