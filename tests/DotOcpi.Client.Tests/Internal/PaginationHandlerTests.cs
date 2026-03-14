using DotOcpi.Client.Internal;
using FluentAssertions;
using Xunit;

namespace DotOcpi.Client.Tests.Internal;

public class PaginationHandlerTests
{
    [Fact]
    public async Task StreamAllAsync_SinglePage_ReturnsAllItems()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            """{"status_code": 1000, "data": [{"id": "LOC1"}, {"id": "LOC2"}], "timestamp": "2024-01-01T00:00:00Z"}""",
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "2" }
        );

        var httpClient = new HttpClient(handler);
        var pagination = new PaginationHandler(httpClient);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://cpo.example.com/ocpi/2.2.1/cpo/locations");
        var items = new List<object>();

        await foreach (var item in pagination.StreamAllAsync(request, OcpiVersion.V2_2_1, typeof(TestItem), "token"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(2);
        handler.SentRequests.Should().HaveCount(1);
    }

    [Fact]
    public async Task StreamAllAsync_MultiplePages_FollowsLinkHeaders()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            """{"status_code": 1000, "data": [{"id": "LOC1"}], "timestamp": "2024-01-01T00:00:00Z"}""",
            headers: new Dictionary<string, string>
            {
                ["X-Total-Count"] = "3",
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=1&limit=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse(
            """{"status_code": 1000, "data": [{"id": "LOC2"}], "timestamp": "2024-01-01T00:00:00Z"}""",
            headers: new Dictionary<string, string>
            {
                ["X-Total-Count"] = "3",
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=2&limit=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse(
            """{"status_code": 1000, "data": [{"id": "LOC3"}], "timestamp": "2024-01-01T00:00:00Z"}""",
            headers: new Dictionary<string, string> { ["X-Total-Count"] = "3" }
        );

        var httpClient = new HttpClient(handler);
        var pagination = new PaginationHandler(httpClient);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://cpo.example.com/ocpi/2.2.1/cpo/locations");
        var items = new List<object>();

        await foreach (var item in pagination.StreamAllAsync(request, OcpiVersion.V2_2_1, typeof(TestItem), "token"))
        {
            items.Add(item);
        }

        items.Should().HaveCount(3);
        handler.SentRequests.Should().HaveCount(3);
    }

    [Fact]
    public async Task StreamAllAsync_FollowUp_AuthHeaderSet()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            """{"status_code": 1000, "data": [{"id": "LOC1"}], "timestamp": "2024-01-01T00:00:00Z"}""",
            headers: new Dictionary<string, string>
            {
                ["Link"] = """<https://cpo.example.com/ocpi/2.2.1/cpo/locations?offset=1>; rel="next" """,
            }
        );
        handler.EnqueueResponse("""{"status_code": 1000, "data": [], "timestamp": "2024-01-01T00:00:00Z"}""");

        var httpClient = new HttpClient(handler);
        var pagination = new PaginationHandler(httpClient);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://cpo.example.com/ocpi/2.2.1/cpo/locations");
        request.Headers.Add("Authorization", "Token dG9rZW4=");

        await foreach (var _ in pagination.StreamAllAsync(request, OcpiVersion.V2_2_1, typeof(TestItem), "my-token"))
        { }

        handler.SentRequests.Should().HaveCount(2);
        var followUpRequest = handler.SentRequests[1];
        followUpRequest.Headers.Authorization.Should().NotBeNull();
        followUpRequest.Headers.Contains("X-Request-ID").Should().BeTrue();
    }

    [Fact]
    public async Task StreamAllAsync_ErrorOnFirstPage_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse(
            """{"status_code": 3000, "status_message": "Server error"}""",
            System.Net.HttpStatusCode.InternalServerError
        );

        var httpClient = new HttpClient(handler);
        var pagination = new PaginationHandler(httpClient);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://cpo.example.com/ocpi/2.2.1/cpo/locations");
        var items = new List<object>();

        await foreach (var item in pagination.StreamAllAsync(request, OcpiVersion.V2_2_1, typeof(TestItem), "token"))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task StreamAllAsync_EmptyDataArray_ReturnsEmpty()
    {
        var handler = new MockHttpMessageHandler();
        handler.EnqueueResponse("""{"status_code": 1000, "data": [], "timestamp": "2024-01-01T00:00:00Z"}""");

        var httpClient = new HttpClient(handler);
        var pagination = new PaginationHandler(httpClient);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://cpo.example.com/ocpi/2.2.1/cpo/locations");
        var items = new List<object>();

        await foreach (var item in pagination.StreamAllAsync(request, OcpiVersion.V2_2_1, typeof(TestItem), "token"))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    private sealed class TestItem
    {
        public string? Id { get; set; }
    }
}
