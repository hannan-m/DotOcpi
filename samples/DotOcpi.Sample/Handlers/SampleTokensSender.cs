using DotOcpi.Modules;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Serves EV driver tokens to CPOs that pull them.
/// Returns a static sample token list.
/// </summary>
public sealed partial class SampleTokensSender(ILogger<SampleTokensSender> logger) : ITokensSender
{
    private readonly ILogger _logger = logger;
    private static readonly IReadOnlyList<object> SampleTokens =
    [
        new
        {
            uid = "TOKEN001",
            type = "RFID",
            auth_id = "NL-MSP-000001",
            issuer = "DotOcpi Sample eMSP",
            valid = true,
            whitelist = "ALWAYS",
            last_updated = "2026-01-15T10:00:00Z",
        },
    ];

    public Task<PaginatedResult<object>> GetTokensAsync(
        OcpiRequestContext context,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int offset,
        int limit,
        CancellationToken ct
    )
    {
        LogGetTokens(context.CpoId, offset, limit);

        var page = SampleTokens.Skip(offset).Take(limit).ToList();
        var result = new PaginatedResult<object>
        {
            Items = page,
            TotalCount = SampleTokens.Count,
            Offset = offset,
            Limit = limit,
        };
        return Task.FromResult(result);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[Tokens] GET tokens for {CpoId} (offset={Offset}, limit={Limit})"
    )]
    private partial void LogGetTokens(string cpoId, int offset, int limit);
}
