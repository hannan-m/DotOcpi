using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Sends charging profile operations to CPO endpoints (OCPI 2.2+ only).
/// </summary>
internal sealed class ChargingProfilesClient : IChargingProfilesClient
{
    private readonly HttpClient _httpClient;
    private readonly OcpiHttpRequestBuilder _requestBuilder;
    private readonly IOutboundTokenProvider _tokenProvider;

    internal ChargingProfilesClient(
        HttpClient httpClient,
        OcpiHttpRequestBuilder requestBuilder,
        IOutboundTokenProvider tokenProvider
    )
    {
        _httpClient = httpClient;
        _requestBuilder = requestBuilder;
        _tokenProvider = tokenProvider;
    }

    public async Task<OcpiResult<object>> SetChargingProfileAsync(
        string cpoId,
        string sessionId,
        object profile,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        if (connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {connection.Version.ToVersionString()}."
            );
        }

        var cpoToken = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(HttpMethod.Put, cpoId, "chargingprofiles", sessionId, cpoToken, profile);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(connection.Version),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<OcpiResult<object>> DeleteChargingProfileAsync(
        string cpoId,
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        if (connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {connection.Version.ToVersionString()}."
            );
        }

        var cpoToken = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(HttpMethod.Delete, cpoId, "chargingprofiles", sessionId, cpoToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(connection.Version),
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task<OcpiResult<object>> GetActiveChargingProfileAsync(
        string cpoId,
        string sessionId,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _requestBuilder.GetConnection(cpoId);
        if (connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {connection.Version.ToVersionString()}."
            );
        }

        var cpoToken = await _tokenProvider.GetTokenAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = _requestBuilder.Build(HttpMethod.Get, cpoId, "chargingprofiles", sessionId, cpoToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(connection.Version),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
