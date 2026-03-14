namespace DotOcpi.Security;

/// <summary>
/// Result of validating an incoming OCPI request token.
/// </summary>
public sealed record TokenValidationResult
{
    private TokenValidationResult(bool isValid, TokenEntry? entry, string? error)
    {
        IsValid = isValid;
        Entry = entry;
        Error = error;
    }

    /// <summary>Whether the token is valid.</summary>
    public bool IsValid { get; }

    /// <summary>The token entry if validation succeeded.</summary>
    public TokenEntry? Entry { get; }

    /// <summary>Error description if validation failed.</summary>
    public string? Error { get; }

    /// <summary>Creates a successful validation result.</summary>
    public static TokenValidationResult Valid(TokenEntry entry) => new(true, entry, null);

    /// <summary>Creates a failed validation result.</summary>
    public static TokenValidationResult Failed(string error) => new(false, null, error);
}
