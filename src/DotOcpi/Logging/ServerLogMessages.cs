using Microsoft.Extensions.Logging;

namespace DotOcpi.Logging;

/// <summary>
/// Source-generated log methods for the DotOcpi.Server.* categories.
/// Zero allocation when the log level is disabled.
/// </summary>
internal static partial class ServerLogMessages
{
    [LoggerMessage(
        EventId = OcpiLogEvents.InboundRequestReceived,
        Level = LogLevel.Debug,
        Message = "Inbound {Method} {Path} from CPO {CpoId} (request: {RequestId})"
    )]
    public static partial void InboundRequestReceived(
        this ILogger logger,
        string method,
        string path,
        string cpoId,
        string requestId
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.InboundRequestProcessed,
        Level = LogLevel.Debug,
        Message = "Processed {Method} {Path} from CPO {CpoId}: OCPI {OcpiStatus}"
    )]
    public static partial void InboundRequestProcessed(
        this ILogger logger,
        string method,
        string path,
        string cpoId,
        int ocpiStatus
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.InboundRequestRejected,
        Level = LogLevel.Warning,
        Message = "Rejected {Method} {Path} from CPO {CpoId}: {Reason}"
    )]
    public static partial void InboundRequestRejected(
        this ILogger logger,
        string method,
        string path,
        string cpoId,
        string reason
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.ConsumerHandlerFailed,
        Level = LogLevel.Error,
        Message = "Consumer handler failed for {Method} {Path} from CPO {CpoId}"
    )]
    public static partial void ConsumerHandlerFailed(
        this ILogger logger,
        Exception exception,
        string method,
        string path,
        string cpoId
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.CpoAdded,
        Level = LogLevel.Information,
        Message = "CPO {CpoId} added to registry (version: {Version})"
    )]
    public static partial void CpoAdded(this ILogger logger, string cpoId, OcpiVersion version);

    [LoggerMessage(
        EventId = OcpiLogEvents.CpoUpdated,
        Level = LogLevel.Information,
        Message = "CPO {CpoId} updated in registry"
    )]
    public static partial void CpoUpdated(this ILogger logger, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.CpoRemoved,
        Level = LogLevel.Information,
        Message = "CPO {CpoId} removed from registry"
    )]
    public static partial void CpoRemoved(this ILogger logger, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.CacheInvalidated,
        Level = LogLevel.Debug,
        Message = "Registry cache invalidated for CPO {CpoId} (reason: {Reason})"
    )]
    public static partial void CacheInvalidated(this ILogger logger, string cpoId, string reason);

    [LoggerMessage(
        EventId = OcpiLogEvents.ValidationFailed,
        Level = LogLevel.Warning,
        Message = "Validation failed for {ModelType}: {ErrorMessage}"
    )]
    public static partial void ValidationFailed(this ILogger logger, string modelType, string errorMessage);

    [LoggerMessage(
        EventId = OcpiLogEvents.VersionMismatch,
        Level = LogLevel.Warning,
        Message = "Version mismatch for CPO {CpoId}: expected {ExpectedVersion}, got {ActualVersion}"
    )]
    public static partial void VersionMismatch(
        this ILogger logger,
        string cpoId,
        string expectedVersion,
        string actualVersion
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.ProbeSuccess,
        Level = LogLevel.Debug,
        Message = "Health probe succeeded for CPO {CpoId}"
    )]
    public static partial void ProbeSuccess(this ILogger logger, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.ProbeFailed,
        Level = LogLevel.Warning,
        Message = "Health probe failed for CPO {CpoId}"
    )]
    public static partial void ProbeFailed(this ILogger logger, Exception exception, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.CpoMarkedOffline,
        Level = LogLevel.Warning,
        Message = "CPO {CpoId} marked as offline after probe failure"
    )]
    public static partial void CpoMarkedOffline(this ILogger logger, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.CpoRecovered,
        Level = LogLevel.Information,
        Message = "CPO {CpoId} recovered from offline status"
    )]
    public static partial void CpoRecovered(this ILogger logger, string cpoId);
}
