using DotOcpi.Modules;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives async command result callbacks from CPOs.
/// When the eMSP sends a command (e.g., StartSession), the CPO responds
/// asynchronously via POST to the response_url with the result.
/// </summary>
public sealed partial class SampleCommandsCallback(ILogger<SampleCommandsCallback> logger) : ICommandsCallback
{
    private readonly ILogger _logger = logger;

    public Task<OcpiResult> OnCommandResultAsync(
        OcpiRequestContext context,
        string correlationId,
        object result,
        CancellationToken ct
    )
    {
        var resultStr = result.ToString() ?? "";
        LogCommandResult(correlationId, context.CpoId, resultStr);
        return Task.FromResult(OcpiResult.Success());
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[Commands] Async result for {CorrelationId} from {CpoId}: {Result}"
    )]
    private partial void LogCommandResult(string correlationId, string cpoId, string result);
}
