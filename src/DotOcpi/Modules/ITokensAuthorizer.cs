namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for real-time token authorization.
/// Handles POST /{token_uid}/authorize from CPOs.
/// </summary>
public interface ITokensAuthorizer
{
    /// <summary>
    /// Authorizes a token for charging at the given location.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="tokenUid">The token UID being authorized.</param>
    /// <param name="locationReferences">Optional location references (version-specific model), or null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Authorization result (version-specific AuthorizationInfo model).</returns>
    Task<OcpiResult<object>> AuthorizeAsync(
        OcpiRequestContext context,
        string tokenUid,
        object? locationReferences,
        CancellationToken ct
    );
}
