namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for receiving CDR (Charge Detail Record) data pushed by CPOs.
/// </summary>
public interface ICdrsReceiver
{
    /// <summary>
    /// Handles a CDR POST from a CPO. Returns the stored CDR and whether it was newly created.
    /// If a CDR with the same ID already exists, return it with <c>isNew = false</c> for idempotency.
    /// </summary>
    Task<OcpiResult<CdrPostResult>> OnCdrPostAsync(OcpiRequestContext context, object data, CancellationToken ct);

    /// <summary>Retrieves a CDR by ID.</summary>
    Task<OcpiResult<object>> GetCdrAsync(OcpiRequestContext context, string cdrId, CancellationToken ct);
}

/// <summary>
/// Result of a CDR POST operation, including whether the CDR was newly created.
/// </summary>
/// <param name="CdrId">The CDR identifier.</param>
/// <param name="IsNew">True if the CDR was newly created, false if it already existed (idempotent).</param>
public sealed record CdrPostResult(string CdrId, bool IsNew);
