namespace DotOcpi.Modules;

/// <summary>
/// Consumer interface for the server-side Credentials endpoint.
/// Handles incoming POST (register), PUT (update), and DELETE (unregister)
/// requests from CPOs.
/// </summary>
public interface ICredentialsHandler
{
    /// <summary>
    /// Handles an initial credentials POST from a CPO (registration).
    /// The consumer should validate the credentials, store the token, and
    /// return the eMSP's own credentials.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="credentials">The CPO's credentials (version-specific model).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The eMSP's credentials to return (version-specific model), or failure.</returns>
    Task<OcpiResult<object>> OnCredentialsPostAsync(
        OcpiRequestContext context,
        object credentials,
        CancellationToken ct
    );

    /// <summary>
    /// Handles a credentials PUT from a CPO (credential rotation).
    /// The consumer should validate the new credentials, rotate the token,
    /// and return the eMSP's updated credentials.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="credentials">The CPO's updated credentials (version-specific model).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The eMSP's credentials to return (version-specific model), or failure.</returns>
    Task<OcpiResult<object>> OnCredentialsPutAsync(
        OcpiRequestContext context,
        object credentials,
        CancellationToken ct
    );

    /// <summary>
    /// Handles a credentials DELETE from a CPO (unregistration).
    /// The consumer should invalidate the connection and revoke tokens.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<OcpiResult> OnCredentialsDeleteAsync(OcpiRequestContext context, CancellationToken ct);

    /// <summary>
    /// Handles a credentials GET from a CPO.
    /// Returns the eMSP's current credentials for this connection.
    /// </summary>
    /// <param name="context">The OCPI request context.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<OcpiResult<object>> GetCredentialsAsync(OcpiRequestContext context, CancellationToken ct);
}
