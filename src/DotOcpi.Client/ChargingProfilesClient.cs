using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Sends charging profile operations to CPO endpoints (OCPI 2.2+ only).
/// </summary>
internal sealed class ChargingProfilesClient : IChargingProfilesClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;

    internal ChargingProfilesClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
    }

    public async Task<OcpiResult<object>> SetChargingProfileAsync(
        string cpoId,
        string sessionId,
        object profile,
        CancellationToken cancellationToken = default
    )
    {
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        if (context.Connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {context.Connection.Version.ToVersionString()}."
            );
        }

        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Put, context, "chargingprofiles", sessionId, profile);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                context.Connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(context.Connection.Version),
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
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        if (context.Connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {context.Connection.Version.ToVersionString()}."
            );
        }

        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Delete, context, "chargingprofiles", sessionId);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                context.Connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(context.Connection.Version),
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
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        if (context.Connection.Version is OcpiVersion.V2_0 or OcpiVersion.V2_1_1)
        {
            return OcpiResult<object>.Failure(
                OcpiStatusCode.GenericClientError,
                $"Charging profiles are not supported in OCPI {context.Connection.Version.ToVersionString()}."
            );
        }

        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Get, context, "chargingprofiles", sessionId);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                context.Connection.Version,
                OcpiModelTypeMap.GetChargingProfileResponseType(context.Connection.Version),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
