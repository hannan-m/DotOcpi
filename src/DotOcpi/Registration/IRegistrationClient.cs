using DotOcpi.Registry;

namespace DotOcpi.Registration;

/// <summary>
/// Orchestrates the OCPI registration handshake with a CPO:
/// version discovery, negotiation, credentials exchange, and
/// registry update.
/// </summary>
public interface IRegistrationClient
{
    /// <summary>
    /// Performs the full registration handshake with a CPO.
    /// </summary>
    /// <param name="request">The registration parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration result including the connection and CPO's raw token.</returns>
    Task<RegistrationResult> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates credentials for an existing CPO connection.
    /// Generates new tokens, sends PUT credentials, and updates the registry.
    /// </summary>
    /// <param name="request">The rotation parameters including the current raw CPO token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rotation result including the updated connection and new CPO token.</returns>
    Task<RegistrationResult> RotateCredentialsAsync(
        CredentialRotationRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Unregisters from a CPO. Sends DELETE credentials and removes from registry.
    /// </summary>
    /// <param name="request">The unregistration parameters including the current raw CPO token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnregisterAsync(UnregisterRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Parameters for initiating a registration handshake with a CPO.
/// </summary>
/// <param name="VersionsUrl">The CPO's versions endpoint URL. Must use HTTPS.</param>
/// <param name="TokenA">The pre-shared Token A for initial authentication.</param>
/// <param name="EmspCountryCode">The eMSP country code to present to this CPO.</param>
/// <param name="EmspPartyId">The eMSP party ID to present to this CPO.</param>
/// <param name="EmspVersionsUrl">The eMSP's own versions endpoint URL. Must use HTTPS.</param>
/// <param name="EmspBusinessName">The eMSP's business name for credentials.</param>
/// <param name="SupportedVersions">
/// Optional filter for supported OCPI versions. If null, all versions are supported.
/// </param>
public sealed record RegistrationRequest(
    string VersionsUrl,
    string TokenA,
    string EmspCountryCode,
    string EmspPartyId,
    string EmspVersionsUrl,
    string EmspBusinessName,
    IReadOnlySet<OcpiVersion>? SupportedVersions = null
);

/// <summary>
/// Result of a successful registration or credential rotation.
/// </summary>
/// <param name="Connection">The CPO connection entry stored in the registry.</param>
/// <param name="CpoToken">
/// The raw token issued by the CPO for the eMSP to use when calling CPO endpoints.
/// The consumer must store this securely (e.g., in a vault or encrypted store).
/// </param>
public sealed record RegistrationResult(CpoConnection Connection, string CpoToken);

/// <summary>
/// Parameters for rotating credentials with an existing CPO connection.
/// </summary>
/// <param name="ConnectionKey">The CPO's connection key ("{country_code}:{party_id}").</param>
/// <param name="CurrentCpoToken">The current raw token from the CPO (used for PUT authentication).</param>
/// <param name="EmspBusinessName">The eMSP's business name for the updated credentials.</param>
public sealed record CredentialRotationRequest(string ConnectionKey, string CurrentCpoToken, string EmspBusinessName);

/// <summary>
/// Parameters for unregistering from a CPO.
/// </summary>
/// <param name="ConnectionKey">The CPO's connection key ("{country_code}:{party_id}").</param>
/// <param name="CurrentCpoToken">The current raw token from the CPO (used for DELETE authentication).</param>
public sealed record UnregisterRequest(string ConnectionKey, string CurrentCpoToken);
