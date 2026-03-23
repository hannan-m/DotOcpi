namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for the Tokens module (eMSP = Sender).
/// The eMSP serves its tokens to CPOs via GET list + pagination.
/// </summary>
public interface ITokensSender
{
    /// <summary>
    /// Returns a page of tokens for a CPO to pull.
    /// </summary>
    Task<PaginatedResult<object>> GetTokensAsync(
        OcpiRequestContext context,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int offset,
        int limit,
        CancellationToken ct
    );
}
