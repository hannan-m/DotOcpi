using System.Runtime.CompilerServices;
using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls Location data from CPO endpoints using the OCPI locations module.
/// </summary>
internal sealed class LocationsClient : ILocationsClient
{
    private readonly HttpClient _httpClient;
    private readonly OcpiHttpRequestBuilder _requestBuilder;
    private readonly IOutboundTokenProvider _tokenProvider;

    internal LocationsClient(
        HttpClient httpClient,
        OcpiHttpRequestBuilder requestBuilder,
        IOutboundTokenProvider tokenProvider
    )
    {
        _httpClient = httpClient;
        _requestBuilder = requestBuilder;
        _tokenProvider = tokenProvider;
    }

    public async Task<OcpiResult<object>> GetLocationAsync(
        string cpoId,
        string locationId,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(HttpMethod.Get, cpoId, "locations", locationId, token);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var modelType = OcpiModelTypeMap.GetLocationType(connection.Version);
        return await OcpiResponseParser
            .ParseVersionedObjectAsync(response, connection.Version, modelType, cancellationToken)
            .ConfigureAwait(false);
    }

    public async IAsyncEnumerable<object> GetAllLocationsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var modelType = OcpiModelTypeMap.GetLocationType(connection.Version);

        var query = QueryStringBuilder.BuildDateFilter(dateFrom, dateTo);
        var request = query is not null
            ? _requestBuilder.BuildWithQuery(HttpMethod.Get, cpoId, "locations", query, token)
            : _requestBuilder.Build(HttpMethod.Get, cpoId, "locations", null, token);

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
