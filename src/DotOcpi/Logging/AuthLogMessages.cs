using Microsoft.Extensions.Logging;

namespace DotOcpi.Logging;

/// <summary>
/// Source-generated log methods for the DotOcpi.Auth category.
/// Zero allocation when the log level is disabled.
/// </summary>
public static partial class AuthLogMessages
{
    [LoggerMessage(
        EventId = OcpiLogEvents.AuthSuccess,
        Level = LogLevel.Debug,
        Message = "Authentication succeeded for request {RequestId} from CPO {CpoId}"
    )]
    public static partial void AuthSuccess(this ILogger logger, string requestId, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.AuthMissingToken,
        Level = LogLevel.Warning,
        Message = "Authentication failed for request {RequestId}: missing or empty Authorization header"
    )]
    public static partial void AuthMissingToken(this ILogger logger, string requestId);

    [LoggerMessage(
        EventId = OcpiLogEvents.AuthInvalidToken,
        Level = LogLevel.Warning,
        Message = "Authentication failed for request {RequestId}: token hash does not match any stored token"
    )]
    public static partial void AuthInvalidToken(this ILogger logger, string requestId);

    [LoggerMessage(
        EventId = OcpiLogEvents.AuthTokenExpired,
        Level = LogLevel.Warning,
        Message = "Authentication failed for request {RequestId}: token is expired or revoked"
    )]
    public static partial void AuthTokenExpired(this ILogger logger, string requestId);

    [LoggerMessage(
        EventId = OcpiLogEvents.AuthCpoNotFound,
        Level = LogLevel.Warning,
        Message = "Authentication failed for request {RequestId}: CPO not found in registry for token hash {TruncatedHash}"
    )]
    public static partial void AuthCpoNotFound(this ILogger logger, string requestId, string truncatedHash);

    [LoggerMessage(
        EventId = OcpiLogEvents.AuthWrongPurpose,
        Level = LogLevel.Warning,
        Message = "Authentication failed for request {RequestId}: expected {ExpectedPurpose} but received {ActualPurpose}"
    )]
    public static partial void AuthWrongTokenPurpose(
        this ILogger logger,
        string requestId,
        string expectedPurpose,
        string actualPurpose
    );
}
