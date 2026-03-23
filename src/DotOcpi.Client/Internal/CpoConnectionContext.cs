using DotOcpi.Registry;

namespace DotOcpi.Client.Internal;

/// <summary>
/// Pre-resolved snapshot of everything needed to make an outbound OCPI request
/// to a specific CPO: the connection metadata and the raw auth token.
/// Cached by <see cref="CpoConnectionContextProvider"/> to avoid per-call
/// lookups against potentially expensive backing stores (SQL, Key Vault, etc.).
/// </summary>
/// <remarks>
/// Intentionally not a <c>record</c> — the default <c>ToString()</c>
/// for records would include <see cref="RawToken"/> in output, which
/// could leak secrets via logging or diagnostics.
/// </remarks>
internal sealed class CpoConnectionContext
{
    public required CpoConnection Connection { get; init; }
    public required string RawToken { get; init; }
}
