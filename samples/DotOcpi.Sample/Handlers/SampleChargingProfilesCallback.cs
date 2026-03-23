using DotOcpi.Modules;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives async charging profile callbacks from CPOs.
/// </summary>
public sealed partial class SampleChargingProfilesCallback(
    ILogger<SampleChargingProfilesCallback> logger
) : IChargingProfilesCallback
{
    private readonly ILogger _logger = logger;
    public Task<OcpiResult> OnChargingProfileResultAsync(
        OcpiRequestContext context, string correlationId, object result, CancellationToken ct)
    {
        LogProfileResult(correlationId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnActiveChargingProfileUpdateAsync(
        OcpiRequestContext context, string sessionId, object activeProfile, CancellationToken ct)
    {
        LogProfileUpdate(sessionId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[ChargingProfiles] Result for {CorrelationId} from {CpoId}")]
    private partial void LogProfileResult(string correlationId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[ChargingProfiles] Active profile update for session {SessionId} from {CpoId}")]
    private partial void LogProfileUpdate(string sessionId, string cpoId);
}
