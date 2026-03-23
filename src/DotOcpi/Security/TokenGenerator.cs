using System.Buffers.Text;
using System.Security.Cryptography;

namespace DotOcpi.Security;

/// <summary>
/// Generates cryptographically secure OCPI tokens (Token B/C).
/// Uses CSPRNG with minimum 64 bytes, base64url-encoded.
/// </summary>
public static class TokenGenerator
{
    /// <summary>
    /// Minimum token length in bytes. OCPI requires tokens to be
    /// sufficiently random to prevent brute-force attacks.
    /// </summary>
    public const int MinTokenBytes = 64;

    /// <summary>
    /// Generates a new cryptographically secure token string.
    /// </summary>
    /// <param name="byteLength">Number of random bytes. Must be >= 64.</param>
    /// <returns>A base64url-encoded token string.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="byteLength"/> is less than 64.</exception>
    public static string Generate(int byteLength = MinTokenBytes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(byteLength, MinTokenBytes);

        Span<byte> buffer = stackalloc byte[byteLength];
        RandomNumberGenerator.Fill(buffer);
#if NET9_0_OR_GREATER
        return Base64Url.EncodeToString(buffer);
#else
        return Convert.ToBase64String(buffer).TrimEnd('=').Replace('+', '-').Replace('/', '_');
#endif
    }
}
