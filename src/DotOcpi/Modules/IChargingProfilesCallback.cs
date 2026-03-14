namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving async charging profile callbacks from CPOs.
/// </summary>
public interface IChargingProfilesCallback
{
    /// <summary>
    /// Handles an async charging profile result callback from a CPO.
    /// </summary>
    Task<OcpiResult> OnChargingProfileResultAsync(
        OcpiRequestContext context,
        string correlationId,
        object result,
        CancellationToken ct);

    /// <summary>
    /// Handles an active charging profile update from a CPO (PUT).
    /// </summary>
    Task<OcpiResult> OnActiveChargingProfileUpdateAsync(
        OcpiRequestContext context,
        string sessionId,
        object activeProfile,
        CancellationToken ct);
}
