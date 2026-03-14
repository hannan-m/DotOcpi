namespace DotOcpi.Client;

/// <summary>
/// Pulls Session data from a CPO's sessions module endpoint.
/// Returns version-specific model types based on the CPO's negotiated version.
/// </summary>
public interface ISessionsClient
{
    /// <summary>
    /// Streams all sessions from a CPO, following pagination Link headers.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="dateFrom">Only return objects modified after this timestamp (inclusive).</param>
    /// <param name="dateTo">Only return objects modified before this timestamp (exclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    IAsyncEnumerable<object> GetAllSessionsAsync(
        string cpoId,
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Sends charging preferences for a session (OCPI 2.2+ only).
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="preferences">The charging preferences (version-specific model type).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Failure with status 2001 if the CPO's version does not support charging preferences.</returns>
    Task<OcpiResult<object>> PutChargingPreferencesAsync(
        string cpoId,
        string sessionId,
        object preferences,
        CancellationToken cancellationToken = default
    );
}
