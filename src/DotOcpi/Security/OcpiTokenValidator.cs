using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace DotOcpi.Security;

/// <summary>
/// Validates incoming OCPI request tokens against the token store.
/// Uses constant-time comparison to prevent timing attacks.
/// </summary>
public sealed class OcpiTokenValidator
{
    private readonly ITokenStore _tokenStore;

    /// <summary>
    /// Creates a new token validator.
    /// </summary>
    /// <param name="tokenStore">The token store to validate against.</param>
    public OcpiTokenValidator(ITokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Validates an incoming token from an Authorization header.
    /// Hashes the token and performs constant-time comparison against stored hashes.
    /// </summary>
    /// <param name="rawToken">The raw token extracted from the Authorization header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result indicating success or failure with reason.</returns>
    public async ValueTask<TokenValidationResult> ValidateAsync(
        string rawToken,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(rawToken))
        {
            return TokenValidationResult.Failed("Token is empty.");
        }

        var incomingHash = TokenHasher.Hash(rawToken);
        var entry = await _tokenStore.FindAsync(incomingHash, cancellationToken).ConfigureAwait(false);

        if (entry is null)
        {
            return TokenValidationResult.Failed("Token not recognized.");
        }

        // Constant-time comparison to prevent timing attacks. Compares the
        // char spans directly as bytes — no heap allocation. Even though we
        // already found the entry by hash lookup, this ensures no timing
        // information leaks about partial hash matches in alternative store
        // implementations that might use prefix search.
        if (!CryptographicOperations.FixedTimeEquals(
                MemoryMarshal.AsBytes(incomingHash.AsSpan()),
                MemoryMarshal.AsBytes(entry.TokenHash.AsSpan())))
        {
            return TokenValidationResult.Failed("Token validation failed.");
        }

        return TokenValidationResult.Valid(entry);
    }
}
