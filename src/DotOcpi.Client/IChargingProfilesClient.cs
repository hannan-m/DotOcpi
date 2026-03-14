namespace DotOcpi.Client;

/// <summary>
/// Sends charging profile operations to a CPO (OCPI 2.2+ only).
/// </summary>
public interface IChargingProfilesClient
{
    /// <summary>Sets a charging profile on a CPO session.</summary>
    Task<OcpiResult<object>> SetChargingProfileAsync(
        string cpoId,
        string sessionId,
        object profile,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes the active charging profile for a session.</summary>
    Task<OcpiResult<object>> DeleteChargingProfileAsync(
        string cpoId,
        string sessionId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Requests the active charging profile for a session.</summary>
    Task<OcpiResult<object>> GetActiveChargingProfileAsync(
        string cpoId,
        string sessionId,
        CancellationToken cancellationToken = default
    );
}
