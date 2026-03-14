using Microsoft.Extensions.Logging;

namespace DotOcpi.Logging;

/// <summary>
/// Source-generated log methods for the DotOcpi.Registration category.
/// Zero allocation when the log level is disabled.
/// </summary>
internal static partial class RegistrationLogMessages
{
    [LoggerMessage(
        EventId = OcpiLogEvents.HandshakeStarted,
        Level = LogLevel.Information,
        Message = "Registration handshake started for CPO {CpoId} at {VersionsUrl}"
    )]
    public static partial void HandshakeStarted(this ILogger logger, string cpoId, string versionsUrl);

    [LoggerMessage(
        EventId = OcpiLogEvents.VersionNegotiated,
        Level = LogLevel.Information,
        Message = "Negotiated OCPI version {Version} with CPO {CpoId}"
    )]
    public static partial void VersionNegotiated(this ILogger logger, string cpoId, OcpiVersion version);

    [LoggerMessage(
        EventId = OcpiLogEvents.RegistrationComplete,
        Level = LogLevel.Information,
        Message = "Registration complete for CPO {CpoId}, negotiated version {Version}"
    )]
    public static partial void RegistrationComplete(this ILogger logger, string cpoId, OcpiVersion version);

    [LoggerMessage(
        EventId = OcpiLogEvents.HandshakeFailed,
        Level = LogLevel.Error,
        Message = "Registration handshake failed for CPO {CpoId}: {Reason}"
    )]
    public static partial void HandshakeFailed(this ILogger logger, string cpoId, string reason);

    [LoggerMessage(
        EventId = OcpiLogEvents.HandshakeFailed,
        Level = LogLevel.Error,
        Message = "Registration handshake failed for CPO {CpoId}"
    )]
    public static partial void HandshakeFailedWithException(this ILogger logger, Exception exception, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.TokenRotationComplete,
        Level = LogLevel.Information,
        Message = "Token rotation complete for CPO {CpoId}"
    )]
    public static partial void TokenRotationComplete(this ILogger logger, string cpoId);

    [LoggerMessage(
        EventId = OcpiLogEvents.Unregistered,
        Level = LogLevel.Information,
        Message = "CPO {CpoId} unregistered successfully"
    )]
    public static partial void Unregistered(this ILogger logger, string cpoId);
}
