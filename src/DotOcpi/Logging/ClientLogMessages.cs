using Microsoft.Extensions.Logging;

namespace DotOcpi.Logging;

/// <summary>
/// Source-generated log methods for the DotOcpi.Client.* categories.
/// Zero allocation when the log level is disabled.
/// </summary>
internal static partial class ClientLogMessages
{
    [LoggerMessage(
        EventId = OcpiLogEvents.RequestSent,
        Level = LogLevel.Debug,
        Message = "OCPI {Method} {Url} -> CPO {CpoId} (version: {Version})"
    )]
    public static partial void RequestSent(
        this ILogger logger,
        string method,
        string url,
        string cpoId,
        string version
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.ResponseReceived,
        Level = LogLevel.Debug,
        Message = "OCPI response from CPO {CpoId}: HTTP {HttpStatus}, OCPI {OcpiStatus}"
    )]
    public static partial void ResponseReceived(this ILogger logger, string cpoId, int httpStatus, int ocpiStatus);

    [LoggerMessage(
        EventId = OcpiLogEvents.RequestFailed,
        Level = LogLevel.Error,
        Message = "OCPI request to CPO {CpoId} failed: {Method} {Url}"
    )]
    public static partial void RequestFailed(
        this ILogger logger,
        Exception exception,
        string cpoId,
        string method,
        string url
    );

    [LoggerMessage(
        EventId = OcpiLogEvents.PaginationPage,
        Level = LogLevel.Debug,
        Message = "Fetched page {PageNumber} from CPO {CpoId}, {ItemCount} items"
    )]
    public static partial void PaginationPage(this ILogger logger, string cpoId, int pageNumber, int itemCount);

    [LoggerMessage(
        EventId = OcpiLogEvents.CircuitBreakerTripped,
        Level = LogLevel.Warning,
        Message = "Circuit breaker tripped for CPO {CpoId}: {Reason}"
    )]
    public static partial void CircuitBreakerTripped(this ILogger logger, string cpoId, string reason);

    [LoggerMessage(
        EventId = OcpiLogEvents.SyncStarted,
        Level = LogLevel.Information,
        Message = "Pull sync started for CPO {CpoId}, module {ModuleId}"
    )]
    public static partial void SyncStarted(this ILogger logger, string cpoId, string moduleId);

    [LoggerMessage(
        EventId = OcpiLogEvents.SyncPageFetched,
        Level = LogLevel.Debug,
        Message = "Pull sync page for CPO {CpoId}, module {ModuleId}: {ItemCount} items"
    )]
    public static partial void SyncPageFetched(this ILogger logger, string cpoId, string moduleId, int itemCount);

    [LoggerMessage(
        EventId = OcpiLogEvents.SyncComplete,
        Level = LogLevel.Information,
        Message = "Pull sync complete for CPO {CpoId}, module {ModuleId}"
    )]
    public static partial void SyncComplete(this ILogger logger, string cpoId, string moduleId);

    [LoggerMessage(
        EventId = OcpiLogEvents.SyncFailed,
        Level = LogLevel.Error,
        Message = "Pull sync failed for CPO {CpoId}, module {ModuleId}"
    )]
    public static partial void SyncFailed(this ILogger logger, Exception exception, string cpoId, string moduleId);
}
