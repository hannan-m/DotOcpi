using DotOcpi.Modules;
using DotOcpi.Sample.Services;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives CDR pushes from CPOs. Stores and retrieves from <see cref="InMemoryDataStore"/>.
/// </summary>
public sealed partial class SampleCdrsReceiver(
    InMemoryDataStore store,
    ILogger<SampleCdrsReceiver> logger
) : ICdrsReceiver
{
    private readonly ILogger _logger = logger;

    public Task<OcpiResult<CdrPostResult>> OnCdrPostAsync(
        OcpiRequestContext context, object data, CancellationToken ct)
    {
        var cdrId = InMemoryDataStore.ExtractId(data) ?? $"CDR-{Guid.NewGuid():N}"[..16];
        var key = InMemoryDataStore.Key(context.CpoId, cdrId);
        var isNew = store.Cdrs.TryAdd(key, data);

        LogCdrPost(context.CpoId, cdrId, isNew);
        return Task.FromResult(OcpiResult<CdrPostResult>.Success(new CdrPostResult(cdrId, isNew)));
    }

    public Task<OcpiResult<object>> GetCdrAsync(
        OcpiRequestContext context, string cdrId, CancellationToken ct)
    {
        var key = InMemoryDataStore.Key(context.CpoId, cdrId);
        if (store.Cdrs.TryGetValue(key, out var cdr))
            return Task.FromResult(OcpiResult<object>.Success(cdr));

        return Task.FromResult(
            OcpiResult<object>.Failure(OcpiStatusCode.GenericClientError, $"CDR '{cdrId}' not found."));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[CDRs] POST CDR from {CpoId}, id={CdrId}, isNew={IsNew}")]
    private partial void LogCdrPost(string cpoId, string cdrId, bool isNew);
}
