using System.Net;
using System.Text;

namespace DotOcpi.Tests.Fixtures;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> for unit-testing HTTP call patterns.
/// Returns a fixed response body and captures the last request for assertions.
/// </summary>
internal sealed class FakeHttpHandler : HttpMessageHandler
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
