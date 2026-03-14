namespace DotOcpi.Security;

/// <summary>
/// Parses OCPI Authorization headers ("Token {base64}") using Span-based
/// parsing for zero allocation in the hot path.
/// </summary>
public static class AuthorizationHeaderParser
{
    private const string TokenPrefix = "Token ";

    /// <summary>
    /// Attempts to extract the token value from an OCPI Authorization header.
    /// </summary>
    /// <param name="headerValue">The full Authorization header value.</param>
    /// <param name="token">The extracted token string, if successful.</param>
    /// <returns>True if the header was valid and the token was extracted.</returns>
    public static bool TryParse(ReadOnlySpan<char> headerValue, out string token)
    {
        token = string.Empty;

        var trimmed = headerValue.Trim();
        if (trimmed.Length <= TokenPrefix.Length)
        {
            return false;
        }

        if (!trimmed[..TokenPrefix.Length].Equals(TokenPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var tokenSpan = trimmed[TokenPrefix.Length..].Trim();
        if (tokenSpan.IsEmpty)
        {
            return false;
        }

        token = tokenSpan.ToString();
        return true;
    }
}
