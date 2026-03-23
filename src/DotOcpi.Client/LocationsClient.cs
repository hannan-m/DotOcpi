using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls Location data from CPO endpoints using the OCPI locations module.
/// </summary>
internal sealed class LocationsClient : ILocationsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;
    private readonly PaginationHandler _pagination;

    internal LocationsClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
        _pagination = new PaginationHandler(httpClient);
    }

    public async Task<OcpiResult<object>> GetLocationAsync(
        string cpoId,
        string locationId,
        CancellationToken cancellationToken = default
    )
    {
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "locations", locationId);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        var modelType = OcpiModelTypeMap.GetLocationType(context.Connection.Version);
        return await OcpiResponseParser
            .ParseVersionedObjectAsync(response, context.Connection.Version, modelType, cancellationToken)
            .ConfigureAwait(false);
    }

    public IAsyncEnumerable<object> GetAllLocationsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    ) => PullClientHelper.StreamAllAsync(
        _contextProvider, _pagination,
        cpoId, "locations", OcpiModelTypeMap.GetLocationType,
        dateFrom, dateTo, cancellationToken);
}
