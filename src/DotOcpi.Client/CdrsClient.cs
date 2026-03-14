using System.Runtime.CompilerServices;
using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls CDR (Charge Detail Record) data from CPO endpoints.
/// </summary>
internal sealed class CdrsClient : ICdrsClient
{
    private readonly HttpClient _httpClient;
    private readonly OcpiHttpRequestBuilder _requestBuilder;
    private readonly IOutboundTokenProvider _tokenProvider;

    internal CdrsClient(
        HttpClient httpClient,
        OcpiHttpRequestBuilder requestBuilder,
        IOutboundTokenProvider tokenProvider
    )
    {
        _httpClient = httpClient;
        _requestBuilder = requestBuilder;
        _tokenProvider = tokenProvider;
    }

    public async IAsyncEnumerable<object> GetAllCdrsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var modelType = OcpiModelTypeMap.GetCdrType(connection.Version);

        var query = QueryStringBuilder.BuildDateFilter(dateFrom, dateTo);
        var request = query is not null
            ? _requestBuilder.BuildWithQuery(HttpMethod.Get, cpoId, "cdrs", query, token)
            : _requestBuilder.Build(HttpMethod.Get, cpoId, "cdrs", null, token);

        var pagination = new PaginationHandler(_httpClient);
        await foreach (
            var item in pagination
                .StreamAllAsync(request, connection.Version, modelType, token, cancellationToken)
                .ConfigureAwait(false)
        )
        {
            yield return item;
        }
    }
}
