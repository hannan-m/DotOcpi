using DotOcpi.Client.Internal;

namespace DotOcpi.Client;

/// <summary>
/// Sends OCPI commands to CPO endpoints.
/// Each command is POSTed to the CPO's commands module endpoint with a command-specific path.
/// </summary>
internal sealed class CommandsClient : ICommandsClient
{
    private readonly HttpClient _httpClient;
    private readonly ICpoConnectionContextProvider _contextProvider;

    internal CommandsClient(HttpClient httpClient, ICpoConnectionContextProvider contextProvider)
    {
        _httpClient = httpClient;
        _contextProvider = contextProvider;
    }

    public Task<OcpiResult<object>> SendStartSessionAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    ) => SendCommandAsync(cpoId, "START_SESSION", command, cancellationToken);

    public Task<OcpiResult<object>> SendStopSessionAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    ) => SendCommandAsync(cpoId, "STOP_SESSION", command, cancellationToken);

    public Task<OcpiResult<object>> SendReserveNowAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    ) => SendCommandAsync(cpoId, "RESERVE_NOW", command, cancellationToken);

    public Task<OcpiResult<object>> SendUnlockConnectorAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    ) => SendCommandAsync(cpoId, "UNLOCK_CONNECTOR", command, cancellationToken);

    public Task<OcpiResult<object>> SendCancelReservationAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    ) => SendCommandAsync(cpoId, "CANCEL_RESERVATION", command, cancellationToken);

    private async Task<OcpiResult<object>> SendCommandAsync(
        string cpoId,
        string commandType,
        object command,
        CancellationToken cancellationToken
    )
    {
        var context = await _contextProvider.ResolveAsync(cpoId, cancellationToken).ConfigureAwait(false);
        var request = OcpiHttpRequestBuilder.Build(HttpMethod.Post, context, "commands", commandType, command);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        return await OcpiResponseParser
            .ParseVersionedObjectAsync(
                response,
                context.Connection.Version,
                OcpiModelTypeMap.GetCommandResponseType(context.Connection.Version),
                cancellationToken
            )
            .ConfigureAwait(false);
    }
}
