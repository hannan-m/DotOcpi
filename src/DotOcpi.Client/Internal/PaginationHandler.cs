using System.Runtime.CompilerServices;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Follows OCPI Link headers to stream paginated results as <see cref="IAsyncEnumerable{T}"/>.
/// Each page is fetched lazily — the next page is only requested when the consumer iterates past the current one.
/// Enforces a maximum page count to prevent infinite pagination from malicious or misconfigured CPOs.
/// </summary>
internal sealed class PaginationHandler
{
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Maximum number of pages to follow before stopping. Prevents infinite loops
    /// from CPOs that return self-referencing or cyclic Link headers.
    /// </summary>
    internal const int MaxPages = 10_000;

    internal PaginationHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Streams all items across paginated OCPI GET responses.
    /// Follows Link headers until no next page is available or <see cref="MaxPages"/> is reached.
    /// </summary>
    internal async IAsyncEnumerable<object> StreamAllAsync(
        HttpRequestMessage firstRequest,
        OcpiVersion version,
        Type itemType,
        string cpoToken,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var request = firstRequest;
        var pageCount = 0;

        while (true)
        {
            if (++pageCount > MaxPages)
                yield break;

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

    /// <summary>
    /// Streams paginated OCPI GET responses as whole pages.
    /// Each yielded <see cref="OcpiPageResult"/> represents one HTTP response.
    /// Stops after <see cref="MaxPages"/> pages.
    /// </summary>
    internal async IAsyncEnumerable<OcpiPageResult> StreamPagesAsync(
        HttpRequestMessage firstRequest,
        OcpiVersion version,
        Type itemType,
        string cpoToken,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var request = firstRequest;
        var pageCount = 0;

        while (true)
        {
            if (++pageCount > MaxPages)
                yield break;

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var page = await OcpiResponseParser
                .ParseListAsync(response, version, itemType, cancellationToken)
                .ConfigureAwait(false);

            if (!page.StatusCode.IsSuccess)
                yield break;

            yield return page;

            if (page.NextLink is null)
                yield break;

            request = OcpiHttpRequestBuilder.BuildForUrl(HttpMethod.Get, page.NextLink, cpoToken);
        }
    }
}
