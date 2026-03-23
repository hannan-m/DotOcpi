using DotOcpi.Client.Sync;
using DotOcpi.Sample.Services;

namespace DotOcpi.Sample.Handlers;

/// <summary>
/// Receives pulled OCPI data from the background sync service.
/// Stores items, records metrics, and broadcasts SSE events.
/// </summary>
public sealed partial class SampleSyncHandler(
    InMemoryDataStore store,
    MetricsCollector metrics,
    SseEventService sse,
    ILogger<SampleSyncHandler> logger
) : IOcpiSyncHandler
{
    private readonly ILogger _logger = logger;

    public Task OnPageReceivedAsync(
        SyncContext context,
        IReadOnlyList<object> items,
        CancellationToken cancellationToken = default
    )
    {
        foreach (var item in items)
        {
            var id = InMemoryDataStore.ExtractId(item);
            if (id is null) continue;

            var key = InMemoryDataStore.Key(context.CpoId, id);
            switch (context.ModuleId)
            {
                case "locations": store.Locations[key] = item; break;
                case "sessions": store.Sessions[key] = item; break;
                case "cdrs": store.Cdrs[key] = item; break;
                case "tariffs": store.Tariffs[key] = item; break;
            }
        }

        return Task.CompletedTask;
    }

    public async Task OnSyncCompletedAsync(
        SyncContext context,
        SyncResult result,
        CancellationToken cancellationToken = default
    )
    {
        LogSyncCompleted(context.CpoId, context.ModuleId, result.ItemCount, result.PageCount, result.Duration);
        metrics.RecordSync(context.CpoId);
        metrics.RecordRequest(context.CpoId);

        await sse.BroadcastAsync("sync-completed", new
        {
            cpoId = context.CpoId,
            module = context.ModuleId,
            items = result.ItemCount,
            duration = result.Duration.ToString(),
        }).ConfigureAwait(false);

        await sse.BroadcastAsync("data-update", new
        {
            locations = store.Locations.Count,
            sessions = store.Sessions.Count,
            tariffs = store.Tariffs.Count,
            cdrs = store.Cdrs.Count,
        }).ConfigureAwait(false);
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "[Sync] Completed {CpoId}/{ModuleId}: {ItemCount} items, {PageCount} pages in {Duration}"
    )]
    private partial void LogSyncCompleted(string cpoId, string moduleId, int itemCount, int pageCount, TimeSpan duration);
}
