using System.Text.Json;

namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving Session data pushed by CPOs.
/// </summary>
public interface ISessionsReceiver
{
    /// <summary>Handles a full Session PUT from a CPO.</summary>
    Task<OcpiResult> OnSessionPutAsync(OcpiRequestContext context, string sessionId, object data, CancellationToken ct);

    /// <summary>Handles a Session PATCH from a CPO.</summary>
    Task<OcpiResult> OnSessionPatchAsync(OcpiRequestContext context, string sessionId, JsonElement patch, CancellationToken ct);

    /// <summary>Retrieves a Session by ID.</summary>
    Task<OcpiResult<object>> GetSessionAsync(OcpiRequestContext context, string sessionId, CancellationToken ct);
}
