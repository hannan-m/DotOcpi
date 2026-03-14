namespace DotOcpi.Client;

/// <summary>
/// Sends OCPI commands to CPOs (StartSession, StopSession, ReserveNow, UnlockConnector, CancelReservation).
/// All commands return a synchronous response indicating acceptance, with async results arriving via callbacks.
/// </summary>
public interface ICommandsClient
{
    /// <summary>Sends a StartSession command to a CPO.</summary>
    Task<OcpiResult<object>> SendStartSessionAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    );

    /// <summary>Sends a StopSession command to a CPO.</summary>
    Task<OcpiResult<object>> SendStopSessionAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    );

    /// <summary>Sends a ReserveNow command to a CPO.</summary>
    Task<OcpiResult<object>> SendReserveNowAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    );

    /// <summary>Sends an UnlockConnector command to a CPO.</summary>
    Task<OcpiResult<object>> SendUnlockConnectorAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    );

    /// <summary>Sends a CancelReservation command to a CPO (2.2.1 only).</summary>
    Task<OcpiResult<object>> SendCancelReservationAsync(
        string cpoId,
        object command,
        CancellationToken cancellationToken = default
    );
}
