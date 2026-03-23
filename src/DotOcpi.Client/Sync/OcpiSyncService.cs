using DotOcpi.Client.Internal;
using DotOcpi.Registry;
using Microsoft.Extensions.Logging;

namespace DotOcpi.Client.Sync;

/// <summary>
/// Pulls data from CPOs for configured modules using incremental sync.
/// Delivers page-level batches to <see cref="IOcpiSyncHandler"/> and
/// updates <see cref="ISyncStateStore"/> only after successful delivery.
/// </summary>
internal sealed partial class OcpiSyncService : IOcpiSyncService
{
    private readonly ICpoRegistry _registry;
    private readonly ISyncStateStore _syncStateStore;
    private readonly IOcpiSyncHandler? _handler;
    private readonly ICpoConnectionContextProvider _contextProvider;
    private readonly PaginationHandler _pagination;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OcpiSyncService> _logger;

    public OcpiSyncService(
        ICpoRegistry registry,
        ISyncStateStore syncStateStore,
        IOcpiSyncHandler? handler,
        ICpoConnectionContextProvider contextProvider,
        PaginationHandler pagination,
        TimeProvider timeProvider,
        ILogger<OcpiSyncService> logger
    )
    {
        _registry = registry;
        _syncStateStore = syncStateStore;
        _handler = handler;
        _contextProvider = contextProvider;
        _pagination = pagination;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SyncFromCpoAsync(
        string cpoId,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default
    )
    {
        var connection = _registry.FindByConnectionKey(cpoId);
        if (connection is null)
        {
            LogCpoNotFound(cpoId);
            return;
        }

        foreach (var moduleId in connection.ModuleEndpoints.Keys)
        {
            if (IsPullableModule(moduleId))
            {
                await SyncModuleCoreAsync(connection, moduleId, since, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<SyncResult> SyncModuleFromCpoAsync(
        string cpoId,
        string moduleId,
        DateTimeOffset? since = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!IsPullableModule(moduleId))
        {
            return new SyncResult
            {
                ItemCount = 0,
                PageCount = 0,
                Duration = TimeSpan.Zero,
                IsSuccess = false,
                ErrorMessage =
                    $"Module '{moduleId}' is not a pullable module. Valid: locations, sessions, cdrs, tariffs.",
            };
        }

        var connection = _registry.FindByConnectionKey(cpoId);
        if (connection is null)
        {
            LogCpoNotFound(cpoId);
            return new SyncResult
            {
                ItemCount = 0,
                PageCount = 0,
                Duration = TimeSpan.Zero,
                IsSuccess = false,
                ErrorMessage = $"CPO '{cpoId}' not found in registry.",
            };
        }

        return await SyncModuleCoreAsync(connection, moduleId, since, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SyncResult> SyncModuleCoreAsync(
        CpoConnection connection,
        string moduleId,
        DateTimeOffset? since,
        CancellationToken cancellationToken
    )
    {
        var cpoId = connection.ConnectionKey;

        var dateFrom =
            since ?? await _syncStateStore.GetLastSyncAsync(cpoId, moduleId, cancellationToken).ConfigureAwait(false);
        var syncStart = _timeProvider.GetUtcNow();

        LogSyncStarted(cpoId, moduleId, dateFrom);

        var syncContext = new SyncContext
        {
            CpoId = cpoId,
            ModuleId = moduleId,
            Version = connection.Version,
            DateFrom = dateFrom,
            SyncStartedAt = syncStart,
        };

        var itemCount = 0;
        var pageCount = 0;

        try
        {
            var typeSelector = GetTypeSelector(moduleId);
            var pages = PullClientHelper.StreamPagesAsync(
                _contextProvider,
                _pagination,
                cpoId,
                moduleId,
                typeSelector,
                dateFrom,
                null,
                cancellationToken
            );

            await foreach (var page in pages.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                pageCount++;
                itemCount += page.Items.Count;

                if (_handler is not null && page.Items.Count > 0)
                {
                    await _handler
                        .OnPageReceivedAsync(syncContext, page.Items, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            var duration = _timeProvider.GetUtcNow() - syncStart;
            var result = new SyncResult
            {
                ItemCount = itemCount,
                PageCount = pageCount,
                Duration = duration,
                IsSuccess = true,
            };

            if (_handler is not null)
            {
                await _handler.OnSyncCompletedAsync(syncContext, result, cancellationToken).ConfigureAwait(false);
            }

            await _syncStateStore.SetLastSyncAsync(cpoId, moduleId, syncStart, cancellationToken).ConfigureAwait(false);

            LogSyncCompleted(cpoId, moduleId, itemCount);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogSyncCancelled(cpoId, moduleId, itemCount);
            throw;
        }
        catch (Exception ex)
        {
            LogSyncFailed(cpoId, moduleId, ex);
            return new SyncResult
            {
                ItemCount = itemCount,
                PageCount = pageCount,
                Duration = _timeProvider.GetUtcNow() - syncStart,
                IsSuccess = false,
                ErrorMessage = "Sync failed. See server logs for details.",
            };
        }
    }

    private static Func<OcpiVersion, Type> GetTypeSelector(string moduleId) =>
        moduleId switch
        {
            "locations" => OcpiModelTypeMap.GetLocationType,
            "sessions" => OcpiModelTypeMap.GetSessionType,
            "cdrs" => OcpiModelTypeMap.GetCdrType,
            "tariffs" => OcpiModelTypeMap.GetTariffType,
            _ => throw new ArgumentException($"Module '{moduleId}' is not a pullable module.", nameof(moduleId)),
        };

    private static bool IsPullableModule(string moduleId) =>
        moduleId is "locations" or "sessions" or "cdrs" or "tariffs";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Sync skipped: CPO '{CpoId}' not found in registry")]
    private partial void LogCpoNotFound(string cpoId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Sync started: {CpoId}/{ModuleId} since {DateFrom}")]
    private partial void LogSyncStarted(string cpoId, string moduleId, DateTimeOffset? dateFrom);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Sync completed: {CpoId}/{ModuleId}, {ItemCount} items pulled"
    )]
    private partial void LogSyncCompleted(string cpoId, string moduleId, int itemCount);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Sync cancelled: {CpoId}/{ModuleId}, {ItemCount} items pulled before cancellation"
    )]
    private partial void LogSyncCancelled(string cpoId, string moduleId, int itemCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sync failed: {CpoId}/{ModuleId}")]
    private partial void LogSyncFailed(string cpoId, string moduleId, Exception exception);
}
