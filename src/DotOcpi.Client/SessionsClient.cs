using System.Runtime.CompilerServices;
using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls Session data from CPO endpoints and sends charging preferences.
/// </summary>
internal sealed class SessionsClient : ISessionsClient
{
    private readonly HttpClient _httpClient;
    private readonly OcpiHttpRequestBuilder _requestBuilder;
    private readonly IOutboundTokenProvider _tokenProvider;

    internal SessionsClient(
        HttpClient httpClient,
        OcpiHttpRequestBuilder requestBuilder,
        IOutboundTokenProvider tokenProvider
    )
    {
        _httpClient = httpClient;
        _requestBuilder = requestBuilder;
        _tokenProvider = tokenProvider;
    }

    public async IAsyncEnumerable<object> GetAllSessionsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var modelType = OcpiModelTypeMap.GetSessionType(connection.Version);

        var query = QueryStringBuilder.BuildDateFilter(dateFrom, dateTo);
        var request = query is not null
            ? _requestBuilder.BuildWithQuery(HttpMethod.Get, cpoId, "sessions", query, token)
            : _requestBuilder.Build(HttpMethod.Get, cpoId, "sessions", null, token);

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

    /// <summary>
    /// Charging preferences require OCPI 2.2 or later.
    /// </summary>
    public async Task<OcpiResult<object>> PutChargingPreferencesAsync(
        string cpoId,
        string sessionId,
        object preferences,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);

        if (connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging preferences are not supported in OCPI {connection.Version.ToVersionString()}."
            );
        }

        var token = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(
            HttpMethod.Put,
            cpoId,
            "sessions",
            $"{sessionId}/charging_preferences",
            token,
            preferences
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(response, connection.Version, typeof(object), cancellationToken)
            .ConfigureAwait(false);
    }
}
