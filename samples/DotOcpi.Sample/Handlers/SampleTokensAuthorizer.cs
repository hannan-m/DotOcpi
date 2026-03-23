using DotOcpi.Modules;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Handles real-time token authorization requests from CPOs.
/// Validates against the known token list in <see cref="SampleTokensSender"/>.
/// </summary>
public sealed partial class SampleTokensAuthorizer(ILogger<SampleTokensAuthorizer> logger) : ITokensAuthorizer
{
    private readonly ILogger _logger = logger;

    // Known token UIDs — in production, query your user database.
    private static readonly HashSet<string> KnownTokens = new(StringComparer.OrdinalIgnoreCase) { "TOKEN001" };

    public Task<OcpiResult<object>> AuthorizeAsync(
        OcpiRequestContext context,
        string tokenUid,
        object? locationReferences,
        CancellationToken ct
    )
    {
        if (KnownTokens.Contains(tokenUid))
        {
            LogAuthorized(tokenUid, context.CpoId);
            var authInfo = new { allowed = "ALLOWED", token = new { uid = tokenUid } };
            return Task.FromResult(OcpiResult<object>.Success(authInfo));
        }

        LogNotFound(tokenUid, context.CpoId);
        var notFound = new { allowed = "NOT_FOUND", token = new { uid = tokenUid } };
        return Task.FromResult(OcpiResult<object>.Success(notFound));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Tokens] Authorized token {TokenUid} from {CpoId}")]
    private partial void LogAuthorized(string tokenUid, string cpoId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[Tokens] Unknown token {TokenUid} from {CpoId} — NOT_FOUND")]
    private partial void LogNotFound(string tokenUid, string cpoId);
}
