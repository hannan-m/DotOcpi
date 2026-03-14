using System.Security.Cryptography;
using System.Text;

namespace DotOcpi.Security;

/// <summary>
/// Computes SHA-256 hashes of OCPI tokens for server-side storage.
/// Raw tokens are never persisted; only hashes are stored.
/// </summary>
public static class TokenHasher
{
    /// <summary>
    /// Computes the SHA-256 hash of a token string.
    /// Uses stackalloc for zero heap allocation on the hash computation.
    /// </summary>
    /// <param name="token">The raw token string.</param>
    /// <returns>The hex-encoded SHA-256 hash (lowercase, 64 characters).</returns>
    public static string Hash(string token)
    {
        var byteCount = Encoding.UTF8.GetByteCount(token);
        Span<byte> tokenBytes = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(token, tokenBytes);

        Span<byte> hash = stackalloc byte[32]; // SHA-256 = 256 bits = 32 bytes
        SHA256.HashData(tokenBytes, hash);

#if NET9_0_OR_GREATER
        return Convert.ToHexStringLower(hash);
#else
        return Convert.ToHexString(hash).ToLowerInvariant();
#endif
    }
}
