namespace DotOcpi.Registration;

/// <summary>
/// Client for the OCPI credentials module — posts, updates, and
/// deletes credentials on a CPO's credentials endpoint.
/// </summary>
public interface ICredentialsClient
{
    /// <summary>
    /// Posts credentials to a CPO's credentials endpoint (initial registration).
    /// Uses Token A for authentication.
    /// </summary>
    /// <param name="credentialsUrl">The CPO's credentials endpoint URL.</param>
    /// <param name="token">The authentication token (Token A for initial registration).</param>
    /// <param name="version">The negotiated OCPI version.</param>
    /// <param name="ourCredentials">The eMSP's credentials to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CPO's credentials response containing Token B.</returns>
    Task<CredentialsResponse> PostCredentialsAsync(
        string credentialsUrl,
        string token,
        OcpiVersion version,
        object ourCredentials,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Updates credentials on a CPO's credentials endpoint (Token rotation).
    /// Uses current Token B for authentication.
    /// </summary>
    /// <param name="credentialsUrl">The CPO's credentials endpoint URL.</param>
    /// <param name="token">The current Token B.</param>
    /// <param name="version">The negotiated OCPI version.</param>
    /// <param name="ourCredentials">The eMSP's updated credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The CPO's credentials response containing Token C (new Token B).</returns>
    Task<CredentialsResponse> PutCredentialsAsync(
        string credentialsUrl,
        string token,
        OcpiVersion version,
        object ourCredentials,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes credentials (unregisters) from a CPO's credentials endpoint.
    /// </summary>
    /// <param name="credentialsUrl">The CPO's credentials endpoint URL.</param>
    /// <param name="token">The current Token B.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteCredentialsAsync(string credentialsUrl, string token, CancellationToken cancellationToken = default);
}
