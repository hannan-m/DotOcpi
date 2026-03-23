namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving async command callback results from CPOs.
/// When the eMSP sends a command (StartSession, StopSession, etc.), the CPO
/// responds asynchronously via POST to the response_url with the result.
/// </summary>
public interface ICommandsCallback
{
    /// <summary>
    /// Handles an async command result callback from a CPO.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="correlationId">The correlation ID that matches the original command.</param>
    /// <param name="result">The command result (version-specific model).</param>
    /// <param name="ct">Cancellation token.</param>
    Task<OcpiResult> OnCommandResultAsync(
        OcpiRequestContext context,
        string correlationId,
        object result,
        CancellationToken ct
    );
}
