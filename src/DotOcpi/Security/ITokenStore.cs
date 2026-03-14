namespace DotOcpi.Security;

/// <summary>
/// Stores and retrieves hashed OCPI tokens. Implementations must be thread-safe.
/// Raw tokens are never stored; only SHA-256 hashes are persisted.
/// </summary>
public interface ITokenStore
{
    /// <summary>
    /// Stores a token hash with its purpose and associated party identity.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash of the token.</param>
    /// <param name="purpose">The token's purpose (A, B, or C).</param>
    /// <param name="partyId">The party identity this token is associated with (e.g. "NL:TNM").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StoreAsync(
        string tokenHash,
        TokenPurpose purpose,
        string partyId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Looks up a token hash and returns its purpose and associated party identity.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token entry if found, or null.</returns>
    ValueTask<TokenEntry?> FindAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a token hash from the store.
    /// </summary>
    /// <param name="tokenHash">The SHA-256 hash to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the hash was found and removed.</returns>
    ValueTask<bool> RemoveAsync(string tokenHash, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a stored token entry.
/// </summary>
/// <param name="TokenHash">The SHA-256 hash of the token.</param>
/// <param name="Purpose">The token's purpose (A, B, or C).</param>
/// <param name="PartyId">The party identity this token is associated with.</param>
public sealed record TokenEntry(string TokenHash, TokenPurpose Purpose, string PartyId);
