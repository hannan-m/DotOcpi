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
    /// <param name="purpose">The token's purpose (A or B).</param>
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

    /// <summary>
    /// Atomically rotates a token: stores the new hash, then removes the old.
    /// Implementations should ensure a brief dual-validity window (both hashes valid)
    /// rather than a zero-validity window (neither valid) that would lock out the CPO.
    /// Database-backed implementations should use a single transaction.
    /// The default implementation calls StoreAsync then RemoveAsync sequentially.
    /// </summary>
    async ValueTask RotateTokenAsync(
        string oldTokenHash,
        string newTokenHash,
        TokenPurpose purpose,
        string partyId,
        CancellationToken cancellationToken = default
    )
    {
        await StoreAsync(newTokenHash, purpose, partyId, cancellationToken).ConfigureAwait(false);
        await RemoveAsync(oldTokenHash, cancellationToken).ConfigureAwait(false);
    }

    // ── Outbound CPO token storage (Token C) ────────────────────────────
    //
    // These methods store opaque (protected) CPO tokens for outbound requests.
    // The caller protects tokens via ITokenProtector before storing and
    // unprotects after retrieval — ITokenStore implementations never see
    // raw tokens.
    //
    // Default implementations are no-op so existing custom ITokenStore
    // implementations compile without changes. Override them to enable
    // automatic outbound token management.

    /// <summary>
    /// Stores a protected CPO token for outbound requests to a specific CPO.
    /// </summary>
    /// <param name="cpoId">The CPO connection key (e.g., "DE:ALL").</param>
    /// <param name="protectedToken">The token after <see cref="ITokenProtector.Protect"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask StoreCpoTokenAsync(string cpoId, string protectedToken, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;

    /// <summary>
    /// Retrieves the protected CPO token for outbound requests.
    /// </summary>
    /// <param name="cpoId">The CPO connection key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The protected token string, or null if not found.</returns>
    ValueTask<string?> GetCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default) =>
        new(default(string?));

    /// <summary>
    /// Removes a stored CPO token (e.g., during unregistration).
    /// </summary>
    /// <param name="cpoId">The CPO connection key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the token was found and removed.</returns>
    ValueTask<bool> RemoveCpoTokenAsync(string cpoId, CancellationToken cancellationToken = default) => new(false);
}

/// <summary>
/// Represents a stored token entry.
/// </summary>
/// <param name="TokenHash">The SHA-256 hash of the token.</param>
/// <param name="Purpose">The token's purpose (A, B, or C).</param>
/// <param name="PartyId">The party identity this token is associated with.</param>
public sealed record TokenEntry(string TokenHash, TokenPurpose Purpose, string PartyId);
