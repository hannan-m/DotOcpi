namespace DotOcpi.Registry;

/// <summary>
/// In-memory registry of connected CPOs with their negotiated versions,
/// module endpoints, and credential state. Thread-safe.
/// Three lookup paths: by connection key, by token hash, by eMSP identity.
/// </summary>
public interface ICpoRegistry
{
    /// <summary>
    /// Finds a CPO connection by its connection key ("{country_code}:{party_id}").
    /// </summary>
    CpoConnection? FindByConnectionKey(string connectionKey);

    /// <summary>
    /// Finds a CPO connection by the Token B hash used for authentication.
    /// </summary>
    CpoConnection? FindByTokenHash(string tokenBHash);

    /// <summary>
    /// Finds all CPO connections associated with a given eMSP identity.
    /// </summary>
    /// <param name="emspCountryCode">The eMSP's country code.</param>
    /// <param name="emspPartyId">The eMSP's party ID.</param>
    IReadOnlyList<CpoConnection> FindByEmspIdentity(string emspCountryCode, string emspPartyId);

    /// <summary>
    /// Returns all registered CPO connections.
    /// </summary>
    IReadOnlyList<CpoConnection> GetAll();

    /// <summary>
    /// Adds or updates a CPO connection. Uses optimistic concurrency:
    /// if the connection already exists, the update is rejected unless
    /// <see cref="CpoConnection.ConcurrencyVersion"/> matches the stored version.
    /// </summary>
    /// <param name="connection">The connection to add or update.</param>
    /// <returns>True if the operation succeeded; false if a concurrency conflict occurred.</returns>
    bool AddOrUpdate(CpoConnection connection);

    /// <summary>
    /// Removes a CPO connection by its connection key.
    /// </summary>
    /// <returns>True if the connection was found and removed.</returns>
    bool Remove(string connectionKey);
}
