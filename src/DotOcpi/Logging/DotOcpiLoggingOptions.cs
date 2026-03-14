namespace DotOcpi.Logging;

/// <summary>
/// Controls OCPI-specific logging behavior such as request/response body
/// logging at Trace level and token sanitization in logged payloads.
/// </summary>
public sealed class DotOcpiLoggingOptions
{
    /// <summary>
    /// When true, OCPI request bodies are logged at Trace level.
    /// Disabled by default because bodies may contain tokens or PII.
    /// </summary>
    public bool EnableRequestBodyLogging { get; set; }

    /// <summary>
    /// When true, OCPI response bodies are logged at Trace level.
    /// Disabled by default because bodies may contain tokens or PII.
    /// </summary>
    public bool EnableResponseBodyLogging { get; set; }

    /// <summary>
    /// Maximum number of characters to log from a request or response body.
    /// Bodies exceeding this length are truncated.
    /// </summary>
    public int MaxBodyLogLength { get; set; } = 4096;

    /// <summary>
    /// When true (default), any field named "token" in logged bodies is
    /// replaced with "[REDACTED]" to prevent accidental credential leakage.
    /// </summary>
    public bool SanitizeTokensInLogs { get; set; } = true;
}
