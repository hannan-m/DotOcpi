using System.Text.Json;
using DotOcpi.Modules;
using DotOcpi.Sample.Services;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives session pushes from CPOs. Stores and retrieves from <see cref="InMemoryDataStore"/>.
/// </summary>
public sealed partial class SampleSessionsReceiver(InMemoryDataStore store, ILogger<SampleSessionsReceiver> logger)
    : ISessionsReceiver
{
    private readonly ILogger _logger = logger;

    public Task<OcpiResult> OnSessionPutAsync(
        OcpiRequestContext context,
        string sessionId,
        object data,
        CancellationToken ct
    )
    {
        store.Sessions[InMemoryDataStore.Key(context.CpoId, sessionId)] = data;
        LogSessionPut(sessionId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult> OnSessionPatchAsync(
        OcpiRequestContext context,
        string sessionId,
        JsonElement patch,
        CancellationToken ct
    )
    {
        LogSessionPatch(sessionId, context.CpoId);
        return Task.FromResult(OcpiResult.Success());
    }

    public Task<OcpiResult<object>> GetSessionAsync(OcpiRequestContext context, string sessionId, CancellationToken ct)
    {
        var key = InMemoryDataStore.Key(context.CpoId, sessionId);
        if (store.Sessions.TryGetValue(key, out var session))
            return Task.FromResult(OcpiResult<object>.Success(session));

        return Task.FromResult(
            OcpiResult<object>.Failure(OcpiStatusCode.GenericClientError, $"Session '{sessionId}' not found.")
        );
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Sessions] PUT session {SessionId} from {CpoId}")]
    private partial void LogSessionPut(string sessionId, string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "[Sessions] PATCH session {SessionId} from {CpoId}")]
    private partial void LogSessionPatch(string sessionId, string cpoId);
}
