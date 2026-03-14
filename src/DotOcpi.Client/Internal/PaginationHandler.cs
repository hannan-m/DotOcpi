using System.Runtime.CompilerServices;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Follows OCPI Link headers to stream paginated results as <see cref="IAsyncEnumerable{T}"/>.
/// Each page is fetched lazily — the next page is only requested when the consumer iterates past the current one.
/// </summary>
internal sealed class PaginationHandler
{
    private readonly HttpClient _httpClient;

    internal PaginationHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Streams all items across paginated OCPI GET responses.
    /// Follows Link headers until no next page is available.
    /// </summary>
    /// <param name="firstRequest">The initial HTTP request for the first page.</param>
    /// <param name="version">The negotiated OCPI version for deserialization.</param>
    /// <param name="itemType">The runtime type of items to deserialize.</param>
    /// <param name="cpoToken">The raw CPO token for authenticating follow-up requests.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    internal async IAsyncEnumerable<object> StreamAllAsync(
        HttpRequestMessage firstRequest,
        OcpiVersion version,
        Type itemType,
        string cpoToken,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var request = firstRequest;

        while (true)
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var page = await OcpiResponseParser
                .ParseListAsync(response, version, itemType, cancellationToken)
                .ConfigureAwait(false);

            if (!page.StatusCode.IsSuccess)
                yield break;

            foreach (var item in page.Items)
            {
                yield return item;
            }

            if (page.NextLink is null)
                yield break;

            request = OcpiHttpRequestBuilder.BuildForUrl(HttpMethod.Get, page.NextLink, cpoToken);
        }
    }
}
