using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Pulls Session data from CPO endpoints and sends charging preferences.
/// </summary>
internal sealed class SessionsClient : ISessionsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;
    private readonly PaginationHandler _pagination;

    internal SessionsClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
        _pagination = new PaginationHandler(httpClient);
    }

    public IAsyncEnumerable<object> GetAllSessionsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    ) =>
        PullClientHelper.StreamAllAsync(
            _contextProvider,
            _pagination,
            cpoId,
            "sessions",
            OcpiModelTypeMap.GetSessionType,
            dateFrom,
            dateTo,
            cancellationToken
        );

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
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);

        if (context.Connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging preferences are not supported in OCPI {context.Connection.Version.ToVersionString()}."
            );
        }

        var request = OcpiHttpRequestBuilder.Build(
            HttpMethod.Put,
            context,
            "sessions",
            $"{sessionId}/charging_preferences",
            preferences
        );

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                context.Connection.Version,
                typeof(System.Text.Json.JsonElement),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
