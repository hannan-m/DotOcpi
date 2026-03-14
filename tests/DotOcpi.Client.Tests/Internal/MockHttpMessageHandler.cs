using System.Net;
using System.Text;

namespace DotOcpi.Client.Tests.Internal;

/// <summary>
/// Mock HTTP message handler that returns pre-configured responses based on request URL.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _sentRequests = [];

    internal IReadOnlyList<HttpRequestMessage> SentRequests => _sentRequests;

    internal void EnqueueResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        IDictionary<string, string>? headers = null
    )
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

        if (headers is not null)
        {
            foreach (var (key, value) in headers)
            {
                response.Headers.TryAddWithoutValidation(key, value);
            }
        }

        _responses.Enqueue(response);
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        _sentRequests.Add(request);

        if (_responses.Count == 0)
        {
            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("""{"status_code": 2003, "status_message": "No more responses"}"""),
                }
            );
        }

        return Task.FromResult(_responses.Dequeue());
    }
}
